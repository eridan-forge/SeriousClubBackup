using Microsoft.Win32;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class SteamDetector
{
    public static string? GetSteamExe()
    {
        using var key =
            Registry.CurrentUser.OpenSubKey(
                @"Software\Valve\Steam");

        var path =
            key?.GetValue("SteamPath")?.ToString();

        if (string.IsNullOrWhiteSpace(path))
            return null;

        var exe =
            Path.Combine(path, "steam.exe");

        return File.Exists(exe)
            ? exe
            : null;
    }

    public static string? GetLibraryFolders()
    {
        using var key =
            Registry.CurrentUser.OpenSubKey(
                @"Software\Valve\Steam");

        var path =
            key?.GetValue("SteamPath")?.ToString();

        if (string.IsNullOrWhiteSpace(path))
            return null;

        var file =
            Path.Combine(
                path,
                "steamapps",
                "libraryfolders.vdf");

        return File.Exists(file)
            ? file
            : null;
    }
}