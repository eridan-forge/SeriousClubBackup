using System.Diagnostics;
using System.Timers;

namespace серьёзный.Инфраструктура;

public sealed class СервисНаблюдателя010 : IDisposable
{
    private readonly System.Timers.Timer таймер;

    public СервисНаблюдателя010()
    {
        таймер = new System.Timers.Timer(5000);
        таймер.Elapsed += Проверить;
    }

    public void Запустить() => таймер.Start();

    public void Остановить() => таймер.Stop();

    private void Проверить(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (AppHost.Host == null)
                return;

            if (AppHost.Host.Services == null)
                throw new Exception("Host остановлен.");
        }
        catch
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                UseShellExecute = true
            });

            Environment.Exit(0);
        }
    }

    public void Dispose()
    {
        таймер.Dispose();
    }
}