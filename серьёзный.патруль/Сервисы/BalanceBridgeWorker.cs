using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class BalanceBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;

    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public BalanceBridgeWorker(
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
                var запрос = AccountBalanceBridgeService.TakeNextPending();

                if (запрос != null && клиент.Подключен)
                {
                    await ОбработатьЗапросAsync(запрос, токен);
                }

                AccountBalanceBridgeService.Cleanup(TimeSpan.FromMinutes(5));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    "BalanceBridgeWorker: " + ошибка);
            }

            try
            {
                await Task.Delay(300, токен);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ОбработатьЗапросAsync(
        BalanceRequestRecord запрос,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ЗапроситьБаланс,
            АккаунтId = запрос.AccountId
        });

        var tcs = new TaskCompletionSource<СетевоеСообщение>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ожидание[сообщение.ИдентификаторСообщения] = tcs;

        var отправлено = await клиент.ОтправитьAsync(сообщение);

        if (!отправлено)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);

            AccountBalanceBridgeService.CompleteRequest(запрос.Id, false, 0, 0, 0);

            return;
        }

        using var таймаут = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        using var связанный =
            CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            if (!ответ.Успешно)
            {
                AccountBalanceBridgeService.CompleteRequest(запрос.Id, false, 0, 0, 0);
                return;
            }

            var данные =
                ответ.ПолучитьДанные<серьёзный.Core.CoreModels.БалансDto>();

            if (данные == null)
            {
                AccountBalanceBridgeService.CompleteRequest(запрос.Id, false, 0, 0, 0);
                return;
            }

            AccountBalanceBridgeService.CompleteRequest(
                запрос.Id, true,
                данные.RemainingSeconds,
                данные.PlayedSeconds,
                данные.SessionCount);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);

            AccountBalanceBridgeService.CompleteRequest(запрос.Id, false, 0, 0, 0);
        }
    }
}