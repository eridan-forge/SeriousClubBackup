using System.Diagnostics;
using System.IO;
using серьёзный.Core.CoreRepair;
using серьёзный.Core.CoreLogs;

namespace серьёзный.Core.CoreLaunch;

public static class LauncherResolver
{
    public static bool Launch(string path)
    {
        try
        {
            string? fixedPath =
                GamePathRepairService.Repair(path);

            if (fixedPath == null)
            {
                LaunchLogger.Write($"Не найден файл: {path}");
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = fixedPath,
                WorkingDirectory =
                    Path.GetDirectoryName(fixedPath),
                UseShellExecute = true
            });

            LaunchLogger.Write($"Запущено: {fixedPath}");

            return true;
        }
        catch (System.Exception ex)
        {
            LaunchLogger.Write(ex.ToString());
            return false;
        }
    }

    public static bool LaunchSteam(int appId)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://rungameid/{appId}",
                UseShellExecute = true
            });

            LaunchLogger.Write($"Steam AppID {appId}");

            return true;
        }
        catch (System.Exception ex)
        {
            LaunchLogger.Write(ex.ToString());
            return false;
        }
    }
}