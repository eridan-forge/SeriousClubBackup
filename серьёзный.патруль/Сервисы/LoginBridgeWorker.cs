using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Патруль.Сеть;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class LoginBridgeWorker
{
    private readonly КлиентПатруля клиент;

    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public LoginBridgeWorker(
        КлиентПатруля клиент,
        КонфигурацияПатруля конфигурация)
    {
        this.клиент = клиент;
        this.конфигурация = конфигурация;

        this.клиент.ПолученоСообщение += ОбработатьОтветAsync;
    }

    private Task ОбработатьОтветAsync(СетевоеСообщение сообщение)
    {
        if (сообщение.Тип == серьёзный.Сеть.ТипСообщения.ОтветНаКоманду &&
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
                var запрос = AccountLoginBridgeService.TakeNextPending();

                if (запрос != null && клиент.Подключен)
                {
                    await ОбработатьЗапросAsync(запрос, токен);
                }

                AccountLoginBridgeService.Cleanup(TimeSpan.FromMinutes(10));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    "LoginBridgeWorker: " + ошибка);
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

    private async Task ОбработатьЗапросAsync(
        LoginRequestRecord запрос,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(серьёзный.Сеть.ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = серьёзный.Сеть.КомандаПК.ЗапроситьВходВАккаунт,
            ЛогинАккаунта = запрос.Login,
            ПарольАккаунта = запрос.Password
        });

        var tcs = new TaskCompletionSource<СетевоеСообщение>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ожидание[сообщение.ИдентификаторСообщения] = tcs;

        var отправлено = await клиент.ОтправитьAsync(сообщение);

        if (!отправлено)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);

            AccountLoginBridgeService.CompleteRequest(
                запрос.Id, false, null, null, 0,
                "Нет соединения с сервером.");

            return;
        }

        using var таймаут = new CancellationTokenSource(TimeSpan.FromSeconds(8));

        using var связанный =
            CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            if (!ответ.Успешно)
            {
                AccountLoginBridgeService.CompleteRequest(
                    запрос.Id, false, null, null, 0,
                    ответ.Ошибка ?? "Ошибка авторизации.");

                return;
            }

            var данные =
                ответ.ПолучитьДанные<серьёзный.Core.CoreModels.РезультатВходаDto>();

            if (данные == null)
            {
                AccountLoginBridgeService.CompleteRequest(
                    запрос.Id, false, null, null, 0,
                    "Сервер вернул пустой ответ.");

                return;
            }

            AccountLoginBridgeService.CompleteRequest(
                запрос.Id, true, данные.AccountId,
                данные.FullName, данные.RemainingSeconds, null);
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);

            AccountLoginBridgeService.CompleteRequest(
                запрос.Id, false, null, null, 0,
                "Сервер не ответил вовремя.");
        }
    }
}