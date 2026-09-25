using Microsoft.Win32;
using System.IO;

namespace серьёзный.Core.CoreServices.Launch;

public static class LauncherResolver
{
    public static LaunchInfo Resolve(string путь)
    {
        if (string.IsNullOrWhiteSpace(путь))
            return Unknown();

        путь = путь.Trim();

        if (путь.StartsWith("steam://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Steam,
                Target = путь
            };

        if (путь.StartsWith("com.epicgames.launcher://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Epic,
                Target = путь
            };

        if (путь.StartsWith("riot://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Riot,
                Target = путь
            };

        if (путь.StartsWith("rockstar://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Rockstar,
                Target = путь
            };

        if (путь.StartsWith("uplay://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Ubisoft,
                Target = путь
            };

        if (путь.StartsWith("battlenet://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.BattleNet,
                Target = путь
            };

        if (путь.StartsWith("xbox://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Xbox,
                Target = путь
            };

        if (путь.StartsWith("eadesktop://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.EA,
                Target = путь
            };

        if (путь.StartsWith("gog://"))
            return new LaunchInfo
            {
                Method = LaunchMethod.Gog,
                Target = путь
            };

        if (File.Exists(путь))
        {
            return new LaunchInfo
            {
                Method = LaunchMethod.Exe,
                Target = путь,
                WorkingDirectory = Path.GetDirectoryName(путь) ?? ""
            };
        }

        var найдено = RegistrySearch(путь);

        if (найдено != null)
            return найдено;

        return Unknown();
    }

    private static LaunchInfo? RegistrySearch(string имя)
    {
        var uninstall =
            Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

        if (uninstall == null)
            return null;

        foreach (var sub in uninstall.GetSubKeyNames())
        {
            var key = uninstall.OpenSubKey(sub);

            if (key == null)
                continue;

            var display =
                key.GetValue("DisplayName") as string;

            var icon =
                key.GetValue("DisplayIcon") as string;

            if (display == null || icon == null)
                continue;

            if (!display.Contains(имя,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (File.Exists(icon))
            {
                return new LaunchInfo
                {
                    Method = LaunchMethod.Exe,
                    Target = icon,
                    WorkingDirectory =
                        Path.GetDirectoryName(icon) ?? ""
                };
            }
        }

        return null;
    }

    private static LaunchInfo Unknown()
    {
        return new LaunchInfo
        {
            Method = LaunchMethod.Unknown
        };
    }
}