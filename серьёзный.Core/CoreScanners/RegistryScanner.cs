using Microsoft.Win32;
using серьёзный.Core.CoreModels;
using System.IO;

namespace серьёзный.Core.CoreScanners;

public static class RegistryScanner
{
    public static List<InstalledGame> Find()
    {
        var список = new List<InstalledGame>();

        Scan(Registry.CurrentUser);
        Scan(Registry.LocalMachine);

        return список;

        void Scan(RegistryKey root)
        {
            using var uninstall =
                root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

            if (uninstall == null)
                return;

            foreach (var sub in uninstall.GetSubKeyNames())
            {
                using var key = uninstall.OpenSubKey(sub);

                if (key == null)
                    continue;

                var name =
                    key.GetValue("DisplayName")?.ToString();

                var icon =
                    key.GetValue("DisplayIcon")?.ToString();

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (string.IsNullOrWhiteSpace(icon))
                    continue;

                var exe =
                    icon.Split(',')[0].Trim('"');

                if (!File.Exists(exe))
                    continue;

                список.Add(new InstalledGame
                {
                    Name = name,
                    Path = exe,
                    Launcher = "Windows"
                });
            }
        }
    }
}