# SteamPath

This is a small utility library for finding the location on disk of applications installed with
Steam. It exposes one function, `SteamPath.Find()`, which takes a Steam app ID (as a string) and
returns its path, or null if the app isn't installed.

```csharp
using SteamPath;

var path = SteamPath.Find(1245620);
Console.WriteLine($"Elden Ring is installed in {path}");
```

You can look up an application's app ID on <https://steamdb.info/apps/>.

## OS Support

This package currently only supports Windows, because it relies on the Windows registry to figure
out where Steam itself is installed. I'd be happy to expand this to support other operating systems
as well if anyone has suggestions for how to locate Steam on them.
