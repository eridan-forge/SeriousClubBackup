using Microsoft.Extensions.Hosting;
using серьёзный.Сервисы;

namespace серьёзный.Инфраструктура;

public class СервисФонаСеансов : BackgroundService
{
    private readonly СервисСеансов сервис;

    public СервисФонаСеансов(
        СервисСеансов сервис)
    {
        this.сервис = сервис;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        сервис.Запустить();

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        сервис.Остановить();

        return base.StopAsync(cancellationToken);
    }
}