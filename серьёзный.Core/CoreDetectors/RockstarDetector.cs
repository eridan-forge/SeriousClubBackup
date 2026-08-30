using System;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class RockstarDetector
{
    public static string? GetLauncher()
    {
        string exe =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles),
                "Rockstar Games",
                "Launcher",
                "Launcher.exe");

        return File.Exists(exe)
            ? exe
            : null;
    }
}