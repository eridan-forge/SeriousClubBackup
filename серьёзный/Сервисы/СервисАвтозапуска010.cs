using System.Diagnostics;

namespace серьёзный.Сервисы;

public class СервисАвтозапуска010
{
    private const string ИмяЗадачи = "SeriousClub Server";

    public bool Установить()
    {
        try
        {
            var exe = Environment.ProcessPath!;

            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments =
                    $"/Create /TN \"{ИмяЗадачи}\" " +
                    $"/TR \"\\\"{exe}\\\"\" " +
                    "/SC ONLOGON " +
                    "/RL HIGHEST " +
                    "/F",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);

            p?.WaitForExit();

            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool Удалить()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments =
                    $"/Delete /TN \"{ИмяЗадачи}\" /F",
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);

            p?.WaitForExit();

            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool Установлен()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments =
                    $"/Query /TN \"{ИмяЗадачи}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(psi);

            p?.WaitForExit();

            return p?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}