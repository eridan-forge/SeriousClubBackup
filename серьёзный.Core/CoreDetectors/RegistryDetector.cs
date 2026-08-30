using Microsoft.Win32;
using System.IO;

namespace серьёзный.Core.CoreDetectors;

public static class RegistryDetector
{
    public static string? Find(string gameName)
    {
        string[] roots =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var root in roots)
        {
            using var key =
                Registry.LocalMachine.OpenSubKey(root);

            if (key == null)
                continue;

            foreach (var sub in key.GetSubKeyNames())
            {
                using var item =
                    key.OpenSubKey(sub);

                var display =
                    item?.GetValue("DisplayName")?.ToString();

                if (display == null)
                    continue;

                if (!display.Contains(gameName,
                    System.StringComparison.OrdinalIgnoreCase))
                    continue;

                return item?.GetValue("InstallLocation")?.ToString();
            }
        }

        return null;
    }
}