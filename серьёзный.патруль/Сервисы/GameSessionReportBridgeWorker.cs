using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class GameSessionReportBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;

    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public GameSessionReportBridgeWorker(
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
        while (!токен.IsCancellationRequested)
        {
            try
            {
                var отчёт = GameSessionReportBridgeService.TakeNextPending();

                if (отчёт != null && клиент.Подключен)
                {
                    await ОбработатьОтчётAsync(отчёт, токен);
                }

                GameSessionReportBridgeService.Cleanup(TimeSpan.FromHours(6));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    "GameSessionReportBridgeWorker: " + ошибка);
            }

            try
            {
                await Task.Delay(500, токен);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ОбработатьОтчётAsync(
        GameSessionReportRecord отчёт,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ОтчётИгровойСессии,
            АккаунтId = отчёт.AccountId,
            СеансId = (int)отчёт.Id,
            ДлительностьСекунд = (int)отчёт.PlayedSeconds
        });

        var tcs = new TaskCompletionSource<СетевоеСообщение>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ожидание[сообщение.ИдентификаторСообщения] = tcs;

        var отправлено = await клиент.ОтправитьAsync(сообщение);

        if (!отправлено)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
            return; // Done остаётся 0 - повторим на следующем цикле
        }

        using var таймаут = new CancellationTokenSource(TimeSpan.FromSeconds(8));

        using var связанный =
            CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            if (ответ.Успешно)
            {
                GameSessionReportBridgeService.MarkDone(отчёт.Id);
            }
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
        }
    }
}