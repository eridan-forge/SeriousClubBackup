using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class PlayerChatBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;
    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public PlayerChatBridgeWorker(Сеть.КлиентПатруля клиент, КонфигурацияПатруля конфигурация)
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
                if (клиент.Подключен)
                {
                    var исходящее = PlayerChatOutboxBridgeService.TakeNextPending();

                    if (исходящее != null)
                        await ОтправитьСообщениеAsync(исходящее, токен);

                    var запросИстории = PlayerChatHistoryBridgeService.TakeNextPending();

                    if (запросИстории != null)
                        await ЗапроситьИсториюAsync(запросИстории.Value, токен);
                }

                PlayerChatOutboxBridgeService.Cleanup(TimeSpan.FromMinutes(10));
                PlayerChatHistoryBridgeService.Cleanup(TimeSpan.FromMinutes(10));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать("PlayerChatBridgeWorker: " + ошибка);
            }

            try
            {
                await Task.Delay(400, токен);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ОтправитьСообщениеAsync(
        PlayerChatOutboxRecord запись,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ОтправитьЛичноеСообщение,
            АккаунтId = запись.From,
            ЦельАккаунтId = запись.To,
            Текст = запись.Text
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
        using var связанный = CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            await tcs.Task.WaitAsync(связанный.Token);

            PlayerChatOutboxBridgeService.MarkDone(запись.Id);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
        }
    }

    private async Task ЗапроситьИсториюAsync(
        (long Id, Guid Me, Guid Friend) запрос,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ЗапроситьИсториюЛичногоЧата,
            АккаунтId = запрос.Me,
            ЦельАккаунтId = запрос.Friend
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
        using var связанный = CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            if (!ответ.Успешно)
                return;

            var данные = ответ.ПолучитьДанные<серьёзный.Core.CoreModels.PlayerChatHistoryDto>();

            if (данные != null)
                PlayerChatHistoryBridgeService.CompleteRequest(запрос.Id, данные);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
        }
    }
}