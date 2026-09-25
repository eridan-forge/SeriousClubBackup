using Microsoft.Win32;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreUbisoft;

public static class UbisoftLibraryScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        using var root =
            Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\WOW6432Node\Ubisoft");

        if (root == null)
            return list;

        foreach (var name in root.GetSubKeyNames())
        {
            using var game = root.OpenSubKey(name);
            if (game == null)
                continue;

            var path =
                game.GetValue("InstallDir")?.ToString();

            if (string.IsNullOrWhiteSpace(path))
                continue;

            list.Add(new GameInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Category = "Ubisoft",
                Launcher = "Ubisoft Connect",
                Path = path
            });
        }

        return list;
    }
}