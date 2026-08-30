using System.Text.Json;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreScanners;

public static class EpicScanner
{
    public static List<InstalledGame> Find()
    {
        var список = new List<InstalledGame>();

        var файл =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "Epic",
                "UnrealEngineLauncher",
                "LauncherInstalled.dat");

        if (!File.Exists(файл))
            return список;

        using var doc =
            JsonDocument.Parse(File.ReadAllText(файл));

        if (!doc.RootElement.TryGetProperty("InstallationList", out var arr))
            return список;

        foreach (var item in arr.EnumerateArray())
        {
            if (!item.TryGetProperty("InstallLocation", out var loc))
                continue;

            var папка = loc.GetString();

            if (string.IsNullOrWhiteSpace(папка))
                continue;

            var exe =
                Directory.GetFiles(папка, "*.exe", SearchOption.AllDirectories)
                         .FirstOrDefault();

            if (exe == null)
                continue;

            список.Add(new InstalledGame
            {
                Name = item.GetProperty("AppName").GetString() ?? "Epic Game",
                Path = exe,
                Launcher = "Epic"
            });
        }

        return список;
    }
}