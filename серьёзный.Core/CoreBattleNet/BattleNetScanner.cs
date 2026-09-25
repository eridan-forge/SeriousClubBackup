using Microsoft.Win32;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreBattleNet;

public static class BattleNetScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        using var root =
            Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Blizzard Entertainment");

        if (root == null)
            return list;

        foreach (var gameName in root.GetSubKeyNames())
        {
            using var game = root.OpenSubKey(gameName);
            if (game == null)
                continue;

            var path =
                game.GetValue("InstallPath")?.ToString();

            if (string.IsNullOrWhiteSpace(path))
                continue;

            list.Add(new GameInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = gameName,
                Category = "Battle.net",
                Launcher = "Battle.net",
                Path = path
            });
        }

        return list;
    }
}