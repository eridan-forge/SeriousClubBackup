using System.Diagnostics;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreRepair;
using System.IO;

namespace серьёзный.Core.CoreLaunch;

public static class GameLaunchService
{
    public static Process? Launch(GameInfo game)
    {
        var path =
            GamePathRepairService.Repair(game.Path);

        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (game.Launcher == "Steam" &&
            !string.IsNullOrWhiteSpace(game.AppId))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"steam://run/{game.AppId}",
                UseShellExecute = true
            });

            return FindSteamGame(game);
        }

        if (game.Launcher == "Rockstar")
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path),
                UseShellExecute = true
            });

            return WaitProcess(path);
        }

        if (game.Launcher == "Riot Client")
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path),
                UseShellExecute = true
            });

            return WaitProcess(path);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path),
            Arguments = game.LaunchArguments,
            UseShellExecute = true
        });

        return WaitProcess(path);
    }

    private static Process? WaitProcess(string exe)
    {
        var name = Path.GetFileNameWithoutExtension(exe);

        for (int i = 0; i < 30; i++)
        {
            var p =
                Process.GetProcessesByName(name)
                    .FirstOrDefault();

            if (p != null)
                return p;

            Thread.Sleep(500);
        }

        return null;
    }

    private static Process? FindSteamGame(GameInfo game)
    {
        if (string.IsNullOrWhiteSpace(game.Path))
            return null;

        return WaitProcess(game.Path);
    }
}