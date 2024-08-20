# SteamPath

This is a small utility library for finding the location on disk of applications installed with
Steam. It exposes one function, `SteamPath.SteamPath.Find()`, which takes a Steam app ID (as a
string) and returns its path, or null if the app isn't installed.

```csharp
var path = SteamPath.SteamPath.Find("1245620");
Console.WriteLine($"Elden Ring is installed in {path}");
```

You can look up an application's app ID on <https://steamdb.info/apps/>.

## OS Support

This package supports multiple OS configurations:

* **Windows**: Works as long as Steam is installed normally.

* **Unix native**: Works as long as Steam is installed in `$HOME/.steam`.

* **Unix under Proton**: Works as long as Steam is installed normally.

* **Unix under WINE**: Works as long as Steam is installed in `/home/$USERNAME/.steam` or
  `/Users/$USERNAME/.steam`. Unfortunately, WINE doesn't have access to the `$HOME` environment
  variable, so this won't work if the user's home directory is in an unusual location.

Note that Windows, Proton, and WINE support require using the windows7.0 build target.
