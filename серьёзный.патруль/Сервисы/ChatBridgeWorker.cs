using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class ChatBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;

    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public ChatBridgeWorker(
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
                if (клиент.Подключен)
                {
                    var исходящее = ChatOutboxBridgeService.TakeNextPending();

                    if (исходящее != null)
                        await ОтправитьСообщениеAsync(исходящее);

                    var запросИстории = ChatHistoryBridgeService.TakeNextPending();

                    if (запросИстории != null)
                        await ЗапроситьИсториюAsync(
                            запросИстории.Value.PcId,
                            запросИстории.Value.Id,
                            токен);
                }

                ChatOutboxBridgeService.Cleanup(TimeSpan.FromMinutes(10));
                ChatHistoryBridgeService.Cleanup(TimeSpan.FromMinutes(10));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    "ChatBridgeWorker: " + ошибка);
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

    private async Task ОтправитьСообщениеAsync(ChatOutboxRequestRecord запись)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Чат);

        сообщение.КомпьютерId = запись.PcId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.СообщениеЧата
        {
            КомпьютерId = запись.PcId,
            Имя = запись.Имя,
            Текст = запись.Текст,
            ОтИгрока = true
        });

        await клиент.ОтправитьAsync(сообщение);

        ChatOutboxBridgeService.MarkDone(запись.Id);
    }

    private async Task ЗапроситьИсториюAsync(
        int pcId,
        long localId,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = pcId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = КомандаПК.ЗапроситьИсториюЧата
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

            var данные = ответ.ПолучитьДанные<ChatHistoryDto>();

            if (данные != null)
                ChatHistoryBridgeService.CompleteRequest(localId, данные);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
        }
    }
}