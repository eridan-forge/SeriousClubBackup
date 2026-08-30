using Microsoft.Win32;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreEA;

public static class EALibraryScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        using var root =
            Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Electronic Arts");

        if (root == null)
            return list;

        foreach (var gameName in root.GetSubKeyNames())
        {
            using var game = root.OpenSubKey(gameName);
            if (game == null)
                continue;

            var install =
                game.GetValue("Install Dir")?.ToString();

            if (string.IsNullOrWhiteSpace(install))
                continue;

            list.Add(new GameInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = gameName,
                Category = "EA",
                Launcher = "EA App",
                Path = install
            });
        }

        return list;
    }
}