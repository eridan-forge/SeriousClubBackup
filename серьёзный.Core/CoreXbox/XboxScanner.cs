using Microsoft.Win32;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreXbox;

public static class XboxScanner
{
    public static List<GameInfo> Scan()
    {
        var list = new List<GameInfo>();

        using var root =
            Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\GamingServices");

        if (root == null)
            return list;

        foreach (var name in root.GetValueNames())
        {
            list.Add(new GameInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Category = "Xbox",
                Launcher = "Xbox",
                Path = ""
            });
        }

        return list;
    }
}