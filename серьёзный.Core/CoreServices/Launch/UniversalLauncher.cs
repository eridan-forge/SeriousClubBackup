using System.Diagnostics;

namespace серьёзный.Core.CoreServices.Launch;

public static class UniversalLauncher
{
    public static bool Launch(string путь)
    {
        var info = LauncherResolver.Resolve(путь);

        try
        {
            switch (info.Method)
            {
                case LaunchMethod.Exe:
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = info.Target,
                        WorkingDirectory = info.WorkingDirectory,
                        UseShellExecute = true
                    });
                    return true;

                case LaunchMethod.Steam:
                case LaunchMethod.Epic:
                case LaunchMethod.Riot:
                case LaunchMethod.Rockstar:
                case LaunchMethod.Ubisoft:
                case LaunchMethod.BattleNet:
                case LaunchMethod.Xbox:
                case LaunchMethod.EA:
                case LaunchMethod.Gog:
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = info.Target,
                        UseShellExecute = true
                    });
                    return true;
            }
        }
        catch
        {
        }

        return false;
    }
}