using Microsoft.Win32;
using серьёзный.Core.CoreModels;
using System.Runtime.Versioning;

namespace серьёзный.Core.CoreRiot;

[SupportedOSPlatform("windows")]
public static class RiotScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        using var root =
            Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Riot Games");

        if (root == null)
            return list;

        foreach (var gameName in root.GetSubKeyNames())
        {
            using var game = root.OpenSubKey(gameName);

            if (game == null)
                continue;

            var path =
                game.GetValue("Path")?.ToString();

            if (string.IsNullOrWhiteSpace(path))
                continue;

            list.Add(new GameInfo
            {
                Name = gameName,
                Category = "Riot",
                Launcher = "Riot Client",
                Path = path
            });
        }

        return list;
    }
}