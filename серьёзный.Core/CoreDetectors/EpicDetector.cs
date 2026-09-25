using System;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class EpicDetector
{
    public static string? GetLauncher()
    {
        var path =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "Epic",
                "EpicGamesLauncher",
                "LauncherInstalled.dat");

        return File.Exists(path)
            ? path
            : null;
    }
}