using System.Text.RegularExpressions;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreScanners;

public static class SteamScanner
{
    public static List<InstalledGame> Find()
    {
        var список = new List<InstalledGame>();

        var steam =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86),
                "Steam");

        if (!Directory.Exists(steam))
            return список;

        var vdf =
            Path.Combine(
                steam,
                "steamapps",
                "libraryfolders.vdf");

        if (!File.Exists(vdf))
            return список;

        var текст = File.ReadAllText(vdf);

        var библиотеки =
            Regex.Matches(текст, "\"path\"\\s+\"([^\"]+)\"");

        foreach (Match библиотека in библиотеки)
        {
            var путь =
                библиотека.Groups[1].Value.Replace("\\\\", "\\");

            var steamapps =
                Path.Combine(путь, "steamapps");

            if (!Directory.Exists(steamapps))
                continue;

            foreach (var manifest in Directory.GetFiles(steamapps, "appmanifest_*.acf"))
            {
                var t = File.ReadAllText(manifest);

                var имя =
                    Regex.Match(t, "\"name\"\\s+\"([^\"]+)\"");

                var dir =
                    Regex.Match(t, "\"installdir\"\\s+\"([^\"]+)\"");

                if (!имя.Success || !dir.Success)
                    continue;

                var папка =
                    Path.Combine(
                        steamapps,
                        "common",
                        dir.Groups[1].Value);

                var exe =
                    Directory.GetFiles(папка, "*.exe", SearchOption.AllDirectories)
                             .FirstOrDefault();

                if (exe == null)
                    continue;

                список.Add(new InstalledGame
                {
                    Name = имя.Groups[1].Value,
                    Path = exe,
                    Launcher = "Steam"
                });
            }
        }

        return список;
    }
}