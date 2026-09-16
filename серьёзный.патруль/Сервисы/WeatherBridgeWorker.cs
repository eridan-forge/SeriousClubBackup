using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class WeatherBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;

    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public WeatherBridgeWorker(
        Сеть.КлиентПатруля клиент,
        КонфигурацияПатруля конфигурация)
    {
        this.клиент = клиент;
        this.конфигурация = конфигурация;

        this.клиент.ПолученоСообщение += ОбработатьОтветAsync;
    }

    private Task ОбработатьОтветAsync(СетевоеСообщение сообщение)
    {
        if (сообщение.Тип == ТипСообщения.ОтветНаКоманду &&
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out var tcs))
        {
            tcs.TrySetResult(сообщение);
        }

        return Task.CompletedTask;
    }

    public async Task ЗапуститьAsync(CancellationToken токен)
    {
        // Погоду экран спрашивает 2 раза в час, поэтому и цикл тут
        // медленный (1 сек), и уборка редкая — лишние DELETE по базе
        // раз в 400 мс этой задаче не нужны.
        var последняяУборка = DateTime.Now;

        while (!токен.IsCancellationRequested)
        {
            try
            {
                if (клиент.Подключен)
                {
                    var id = WeatherBridgeService.TakeNextPending();

                    if (id.HasValue)
                        await ЗапроситьПогодуAsync(id.Value, токен);
                }

                if (DateTime.Now - последняяУборка > TimeSpan.FromMinutes(15))
                {
                    WeatherBridgeService.Cleanup(TimeSpan.FromHours(2));
                    последняяУборка = DateTime.Now;
                }
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    "WeatherBridgeWorker: " + ошибка);
            }

            try
            {
                await Task.Delay(1000, токен);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ЗапроситьПогодуAsync(long localId, CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = КомандаПК.ЗапроситьПогоду
        });

        var tcs = new TaskCompletionSource<СетевоеСообщение>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ожидание[сообщение.ИдентификаторСообщения] = tcs;

        var отправлено = await клиент.ОтправитьAsync(сообщение);

        if (!отправлено)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
            return;
        }

        using var таймаут = new CancellationTokenSource(TimeSpan.FromSeconds(8));

        using var связанный =
            CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            if (!ответ.Успешно)
                return;

            var данные = ответ.ПолучитьДанные<WeatherDto>();

            if (данные != null)
                WeatherBridgeService.CompleteRequest(localId, данные);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
        }
    }
}