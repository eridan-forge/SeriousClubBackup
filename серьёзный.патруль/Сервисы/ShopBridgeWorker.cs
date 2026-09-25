using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreServices;
using серьёзный.Core.CoreModels;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class ShopBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;

    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public ShopBridgeWorker(
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
                    var покупка = ShopPurchaseBridgeService.TakeNextPending();

                    if (покупка != null)
                        await ОбработатьПокупкуAsync(покупка, токен);

                    var заказы = ShopOrdersBridgeService.TakeNextPending();

                    if (заказы != null)
                        await ОбработатьЗаказыAsync(заказы.Value.AccountId, заказы.Value.Id, токен);
                }

                ShopPurchaseBridgeService.Cleanup(TimeSpan.FromMinutes(10));
                ShopOrdersBridgeService.Cleanup(TimeSpan.FromMinutes(10));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    "ShopBridgeWorker: " + ошибка);
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

    private async Task ОбработатьПокупкуAsync(
        ShopPurchaseRequestRecord запрос,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = запрос.PcId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ЗапроситьПокупку,
            АккаунтId = запрос.AccountId,
            ItemId = запрос.ItemId,
            Delivery = запрос.Delivery
        });

        var tcs = new TaskCompletionSource<СетевоеСообщение>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ожидание[сообщение.ИдентификаторСообщения] = tcs;

        var отправлено = await клиент.ОтправитьAsync(сообщение);

        if (!отправлено)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
            ShopPurchaseBridgeService.CompleteRequest(запрос.Id, false, null, "Нет соединения с сервером.");
            return;
        }

        using var таймаут = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var связанный = CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            if (!ответ.Успешно)
            {
                ShopPurchaseBridgeService.CompleteRequest(запрос.Id, false, null, ответ.Ошибка ?? "Ошибка магазина.");
                return;
            }

            var данные = ответ.ПолучитьДанные<ShopPurchaseResultDto>();

            if (данные == null || !данные.Success)
            {
                ShopPurchaseBridgeService.CompleteRequest(запрос.Id, false, null, данные?.Error ?? "Сервер вернул пустой ответ.");
                return;
            }

            ShopPurchaseBridgeService.CompleteRequest(запрос.Id, true, данные.RequestId, null);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
            ShopPurchaseBridgeService.CompleteRequest(запрос.Id, false, null, "Сервер не ответил вовремя.");
        }
    }

    private async Task ОбработатьЗаказыAsync(
        Guid accountId, long localId, CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ЗапроситьМоиЗаказы,
            АккаунтId = accountId
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

            var данные = ответ.ПолучитьДанные<ShopOrdersDto>();

            if (данные != null)
                ShopOrdersBridgeService.CompleteRequest(localId, данные);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);
        }
    }
}