using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
#if WINDOWS7_0_OR_GREATER
using Microsoft.Win32;
#endif

namespace SteamPath
{
    public class SteamPath
    {
        /// <summary>
        /// A list of all library directories in which the user might have games installed.
        /// </summary>
        private static readonly Lazy<List<string>> libraries =
            new(() =>
            {
#if WINDOWS7_0_OR_GREATER
                var steam = (string?)
                    Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null);

                if (steam == null)
                {
                    if (!isWine!.Value)
                    {
                        throw new SteamPathException("Steam is not installed.");
                    }

                    var username =
                        Environment.GetEnvironmentVariable("USERNAME")
                        ?? throw new SteamPathException(
                            "USERNAME environment variable unavailable."
                        );

                    var linuxStyle = $"/home/{username}/.steam/root";
                    var macOSStyle = $"/Users/{username}/.steam/root";
                    steam =
                        DirIfExists(ConvertWinePath(linuxStyle))
                        ?? DirIfExists(ConvertWinePath(macOSStyle))
                        ?? throw new SteamPathException(
                            $"Steam is not installed in {linuxStyle} or {macOSStyle}."
                        );
                }
#else
                var home =
                    Environment.GetEnvironmentVariable("HOME")
                    ?? throw new SteamPathException("HOME environment variable unavailable.");
                var steam =
                    DirIfExists($"{home}/.steam/root")
                    ?? throw new SteamPathException($"Steam is not installed in {steam}.");
#endif

                var libraries = new List<string>(new[] { steam });

                VProperty vdf;
                try
                {
                    vdf = VdfConvert.Deserialize(
                        File.ReadAllText(Path.Join(steam, "steamapps", "libraryfolders.vdf"))
                    );
                }
                catch (IOException e)
                {
                    throw new SteamPathException(
                        "Steam installation doesn't contain libraryfolders.vdf.",
                        e
                    );
                }

                libraries.AddRange(
                    vdf.Value.Cast<VProperty>()
                        .Select(child => child.Value["path"]!.Value<string>())
                        .Select(ConvertWinePath)
                );
                return libraries;
            });

        /// <summary>Whether the current process is running under Wine.</summary>
#if WINDOWS7_0_OR_GREATER
        private static readonly Lazy<bool> isWine =
            new(() => Registry.LocalMachine.OpenSubKey(@"Software\Wine") != null);
#else
        private static readonly Lazy<bool> isWine = new(() => false);
#endif

        /// <param name="appID">
        /// The numeric ID of the app to find. You can find the ID for a given app on
        /// https://steamdb.info/apps/.
        /// </param>
        /// <returns>
        /// The path on disk to the Steam application with the given <paramref name="appID"/>, or
        /// null if no such app is installed.
        /// </returns>
        /// <exception cref="SteamPathException">
        /// If Steam itself is not installed or is somehow corrupted.
        /// </exception>
        public static string? Find(string appID)
        {
            string? manifest = FindManifest(appID);
            if (manifest == null)
            {
                return null;
            }

            var vdf = VdfConvert.Deserialize(File.ReadAllText(manifest));
            var appDir = Path.Join(
                Path.GetDirectoryName(manifest),
                "common",
                vdf.Value["installdir"]!.Value<string>()
            );
            return Directory.Exists(appDir) ? appDir : null;
        }

        /// <param name="appID">
        /// The numeric ID of the app to find. You can find the ID for a given app on
        /// https://steamdb.info/apps/.
        /// </param>
        /// <returns>The manifest ACF file for the given <paramref name="appID"/>.</returns>
        private static string? FindManifest(string appID)
        {
            var name = $@"appmanifest_{appID}.acf";
            return libraries
                .Value.Select(library => Path.Join(library, "steamapps", name))
                .Where(File.Exists)
                .FirstOrDefault();
        }

        /// <summary>
        /// If we're running under Wine, converts a Linux <paramref name="path"/> to Windows.
        /// Otherwise, returns <paramref name="path"/> as-is.
        /// </summary>
        private static string ConvertWinePath(string path)
        {
            if (!isWine.Value)
            {
                return path;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winepath.exe",
                        Arguments = "--windows \"" + Regex.Replace(path, @"(\\+)$", @"$1$1") + "\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    },
                };
                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch (Win32Exception e)
            {
                throw new SteamPathException("You're using Wine, but winepath.exe failed.", e);
            }
        }

        /// <returns><paramref name="dir"/> if it exists, or null otherwise.</returns>
        private static string? DirIfExists(string dir) => Directory.Exists(dir) ? dir : null;

        /// <summary>An exception thrown when discovering an application's path fails.</summary>
        public class SteamPathException : Exception
        {
            public SteamPathException()
                : base() { }

            public SteamPathException(string message)
                : base(message) { }

            public SteamPathException(string message, Exception wrapped)
                : base(message, wrapped) { }
        }
    }
}
