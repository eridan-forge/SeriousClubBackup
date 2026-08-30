using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using серьёзный.Сервисы;
using серьёзный.Сеть;
using System.IO;


namespace серьёзный.Инфраструктура;

public static class AppHost
{
    public static IHost? Host { get; private set; }

    public static async Task ЗапуститьAsync()
    {
        Log.Logger =
            new LoggerConfiguration()
                .WriteTo.File(
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.CommonApplicationData),
                        "SeriousClub",
                        "logs",
                        "server-.log"),
                    rollingInterval:
                        RollingInterval.Day)
                .CreateLogger();

        Host =
            Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()

                .UseSerilog()

                .ConfigureServices(services =>
                {
                    services.AddSingleton<СервисСеансов>();

                    services.AddSingleton<СервисНаблюдателя010>();

                    services.AddHostedService<СервисФонаСеансов>();

                    services.AddSingleton<МаякСервера>();

                    services.AddSingleton(_ =>
                        new СерверСвязи(47821));
                })

                .Build();

        await Host.StartAsync();

        Host.Services
    .GetRequiredService<СервисНаблюдателя010>()
    .Запустить();
    }

    public static async Task ОстановитьAsync()
    {
        if (Host == null)
            return;

        await Host.StopAsync();

        Host.Services
    .GetRequiredService<СервисНаблюдателя010>()
    .Остановить();

        Host.Dispose();

        Host = null;
    }
}