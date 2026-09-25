using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreServices;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;

namespace серьёзный.Патруль.Сервисы;

public class EconomyBridgeWorker
{
    private readonly Сеть.КлиентПатруля клиент;
    private readonly КонфигурацияПатруля конфигурация;

    private readonly ConcurrentDictionary<string, TaskCompletionSource<СетевоеСообщение>>
        ожидание = new();

    public EconomyBridgeWorker(Сеть.КлиентПатруля клиент, КонфигурацияПатруля конфигурация)
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
                    var запрос = EconomyBridgeService.TakeNextPending();

                    if (запрос != null)
                        await ОбработатьЗапросAsync(запрос.Value, токен);
                }

                EconomyBridgeService.Cleanup(TimeSpan.FromMinutes(10));
            }
            catch (Exception ошибка)
            {
                серьёзный.патруль.Сервисы.Лог.Записать("EconomyBridgeWorker: " + ошибка);
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

    private async Task ОбработатьЗапросAsync(
        (long Id, Guid AccountId, серьёзный.Core.CoreModels.EconomyRequestDto Request) запрос,
        CancellationToken токен)
    {
        var сообщение = СетевоеСообщение.Создать(ТипСообщения.Команда);

        сообщение.КомпьютерId = конфигурация.КомпьютерId;

        сообщение.УстановитьДанные(new серьёзный.Сеть.КомандаПатрулю
        {
            Команда = КомандаПК.ЗапроситьЭкономику,
            АккаунтId = запрос.AccountId,
            Параметры = System.Text.Json.JsonSerializer.Serialize(запрос.Request)
        });

        var tcs = new TaskCompletionSource<СетевоеСообщение>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        ожидание[сообщение.ИдентификаторСообщения] = tcs;

        var отправлено = await клиент.ОтправитьAsync(сообщение);

        if (!отправлено)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);

            EconomyBridgeService.CompleteRequest(запрос.Id, new серьёзный.Core.CoreModels.EconomyResultDto
            {
                Success = false,
                Error = "Нет соединения с сервером."
            });

            return;
        }

        using var таймаут = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var связанный = CancellationTokenSource.CreateLinkedTokenSource(токен, таймаут.Token);

        try
        {
            var ответ = await tcs.Task.WaitAsync(связанный.Token);

            var результат = ответ.Успешно
                ? ответ.ПолучитьДанные<серьёзный.Core.CoreModels.EconomyResultDto>()
                : new серьёзный.Core.CoreModels.EconomyResultDto { Success = false, Error = ответ.Ошибка };

            EconomyBridgeService.CompleteRequest(запрос.Id, результат ??
                new серьёзный.Core.CoreModels.EconomyResultDto { Success = false, Error = "Пустой ответ." });
        }
        catch (OperationCanceledException)
        {
            ожидание.TryRemove(сообщение.ИдентификаторСообщения, out _);

            EconomyBridgeService.CompleteRequest(запрос.Id, new серьёзный.Core.CoreModels.EconomyResultDto
            {
                Success = false,
                Error = "Сервер не ответил вовремя."
            });
        }
    }
}