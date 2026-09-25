using Microsoft.Win32;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreRockstar;

public static class RockstarLibraryScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        string[] keys =
        {
            @"SOFTWARE\Rockstar Games",
            @"SOFTWARE\WOW6432Node\Rockstar Games"
        };

        foreach (var keyPath in keys)
        {
            using var root = Registry.LocalMachine.OpenSubKey(keyPath);
            if (root == null)
                continue;

            foreach (var gameName in root.GetSubKeyNames())
            {
                using var game = root.OpenSubKey(gameName);
                if (game == null)
                    continue;

                var install =
                    game.GetValue("InstallFolder")?.ToString() ??
                    game.GetValue("InstallDir")?.ToString();

                if (string.IsNullOrWhiteSpace(install))
                    continue;

                list.Add(new GameInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = gameName,
                    Category = "Rockstar",
                    Launcher = "Rockstar",
                    Path = install
                });
            }
        }

        return list;
    }
}