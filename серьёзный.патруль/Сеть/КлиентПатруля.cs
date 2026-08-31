using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.патруль.Сервисы;
using серьёзный.Патруль.Модели;
using серьёзный.Сеть;
using серьёзный.Core.CoreChat;
using серьёзный.Патруль.Система;

namespace серьёзный.Патруль.Сеть
{
    public class КлиентПатруля : IDisposable
    {
        private readonly КонфигурацияПатруля конфигурация;

        private TcpClient? клиент;
        private NetworkStream? поток;
        private StreamReader? читатель;
        private StreamWriter? писатель;

        private readonly SemaphoreSlim блокировкаОтправки =
            new(1, 1);

        private readonly object блокировкаСостояния =
            new();

        private int задержкаПовтора = 1000;

        private bool остановлен;

        public bool Подключен
        {
            get
            {
                lock (блокировкаСостояния)
                {
                    return клиент?.Connected == true;
                }
            }
        }

        public event Func<
            СетевоеСообщение,
            Task>? ПолученоСообщение;

        public event Action? СоединениеПотеряно;


        public КлиентПатруля(
            КонфигурацияПатруля конфигурация)
        {
            this.конфигурация =
                конфигурация
                ?? throw new ArgumentNullException(
                    nameof(конфигурация));
        }


        // =========================================================
        // ЗАПУСК И ПЕРЕПОДКЛЮЧЕНИЕ
        // =========================================================

        public async Task ЗапуститьAsync(
            CancellationToken токен)
        {
            остановлен = false;

            while (!токен.IsCancellationRequested &&
                   !остановлен)
            {
                try
                {
                    await ПодключитьсяAsync(
                        токен);

                    задержкаПовтора = 1000;

                    ЗаписатьЛог(
                        $"Патруль подключён к серверу " +
                        $"{конфигурация.СерверIP}:{конфигурация.СерверПорт}");

                    await ЦиклСвязиAsync(
                        токен);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ошибка)
                {
                    ЗаписатьЛог(
                        "Ошибка соединения Патруля: " +
                        ошибка);

                    if (!токен.IsCancellationRequested &&
                        !остановлен)
                    {
                        try
                        {
                            СоединениеПотеряно?.Invoke();
                        }
                        catch (Exception ошибкаСобытия)
                        {
                            ЗаписатьЛог(
                                "Ошибка события СоединениеПотеряно: " +
                                ошибкаСобытия);
                        }
                    }
                }
                finally
                {
                    ЗакрытьСоединение();
                }

                if (токен.IsCancellationRequested ||
                    остановлен)
                {
                    break;
                }

                try
                {
                    await Task.Delay(
                        задержкаПовтора,
                        токен);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                задержкаПовтора =
                    Math.Min(
                        задержкаПовтора * 2,
                        15000);
            }
        }


        // =========================================================
        // HANDSHAKE + ПОДКЛЮЧЕНИЕ
        // =========================================================

        private async Task ПодключитьсяAsync(
            CancellationToken токен)
        {
            ЗакрытьСоединение();

            TcpClient новыйКлиент =
                new();

            NetworkStream? новыйПоток = null;
            StreamReader? новыйЧитатель = null;
            StreamWriter? новыйПисатель = null;

            using var таймер =
                CancellationTokenSource.CreateLinkedTokenSource(
                    токен);

            таймер.CancelAfter(
                TimeSpan.FromSeconds(
                    Math.Max(
                        1,
                        конфигурация
                            .ТаймаутПодключенияСекунд)));

            try
            {
                await новыйКлиент.ConnectAsync(
                    конфигурация.СерверIP,
                    конфигурация.СерверПорт,
                    таймер.Token);

                новыйПоток =
                    новыйКлиент.GetStream();

                новыйЧитатель =
                    new StreamReader(
                        новыйПоток,
                        new UTF8Encoding(false),
                        detectEncodingFromByteOrderMarks: true,
                        bufferSize: 4096,
                        leaveOpen: true);

                новыйПисатель =
                    new StreamWriter(
                        новыйПоток,
                        new UTF8Encoding(false),
                        bufferSize: 4096,
                        leaveOpen: true)
                    {
                        AutoFlush = true
                    };

                lock (блокировкаСостояния)
                {
                    клиент = новыйКлиент;
                    поток = новыйПоток;
                    читатель = новыйЧитатель;
                    писатель = новыйПисатель;
                }

                // -------------------------------------------------
                // HANDSHAKE
                // -------------------------------------------------

                var приветствие =
                    СетевоеСообщение.Создать(
                        серьёзный.Сеть.ТипСообщения.Приветствие);

                приветствие.КомпьютерId =
                    конфигурация.КомпьютерId;

                приветствие.ИмяКомпьютера =
                    конфигурация.ИмяКомпьютера;

                приветствие.ИмяWindows =
                    Environment.MachineName;

                приветствие.УстановитьДанные(
                    new серьёзный.Сеть.ДанныеHandshake
                    {
                        КомпьютерId =
                            конфигурация.КомпьютерId,

                        ИмяКомпьютера =
                            string.IsNullOrWhiteSpace(
                                конфигурация.ИмяКомпьютера)
                                ? Environment.MachineName
                                : конфигурация.ИмяКомпьютера,

                        ИмяWindows =
                            Environment.MachineName,

                        МАСАдрес =
                         ИдентификацияПК.ПолучитьMAC(),


                        ВерсияПатруля =
                            "1.0.0"
                    });

                var отправлено =
                    await ОтправитьAsync(
                        приветствие);

                if (!отправлено)
                {
                    throw new IOException(
                        "Не удалось отправить handshake.");
                }

                var ответ =
                    await новыйЧитатель.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(
                        ответ))
                {
                    throw new IOException(
                        "Сервер не ответил на handshake.");
                }

                СетевоеСообщение? сообщениеОтвет;

                try
                {
                    сообщениеОтвет =
                        JsonSerializer.Deserialize<
                            СетевоеСообщение>(
                                ответ);
                }
                catch (JsonException ошибка)
                {
                    throw new IOException(
                        "Сервер отправил некорректный handshake.",
                        ошибка);
                }

                if (сообщениеОтвет == null)
                {
                    throw new IOException(
                        "Handshake сервера пуст.");
                }

                if (сообщениеОтвет.Тип !=
                    серьёзный.Сеть.ТипСообщения.ПриветствиеОтвет)
                {
                    throw new IOException(
                        "Сервер ответил неожиданным типом сообщения: " +
                        сообщениеОтвет.Тип);
                }

                if (!сообщениеОтвет.Успешно)
                {
                    throw new IOException(
                        сообщениеОтвет.Ошибка ??
                        "Сервер отклонил подключение.");
                }

                if (сообщениеОтвет.КомпьютерId.HasValue &&
                    сообщениеОтвет.КомпьютерId.Value !=
                    конфигурация.КомпьютерId)
                {
                    throw new IOException(
                        "Сервер вернул другой идентификатор ПК.");
                }
            }
            catch
            {
                try
                {
                    новыйПисатель?.Dispose();
                }
                catch
                {
                }

                try
                {
                    новыйЧитатель?.Dispose();
                }
                catch
                {
                }

                try
                {
                    новыйПоток?.Dispose();
                }
                catch
                {
                }

                try
                {
                    новыйКлиент.Dispose();
                }
                catch
                {
                }

                lock (блокировкаСостояния)
                {
                    if (ReferenceEquals(
                            клиент,
                            новыйКлиент))
                    {
                        клиент = null;
                        поток = null;
                        читатель = null;
                        писатель = null;
                    }
                }

                throw;
            }
        }


        // =========================================================
        // ОСНОВНОЙ ЦИКЛ СВЯЗИ
        // =========================================================

        private async Task ЦиклСвязиAsync(
            CancellationToken токен)
        {
            using var токенСвязи =
                CancellationTokenSource.CreateLinkedTokenSource(
                    токен);

            var задачаЧтения =
                ЧитатьСообщенияAsync(
                    токенСвязи.Token);

            var задачаHeartbeat =
                ОтправлятьHeartbeatAsync(
                    токенСвязи.Token);

            try
            {
                await Task.WhenAny(
                    задачаЧтения,
                    задачаHeartbeat);

                токенСвязи.Cancel();

                try
                {
                    await Task.WhenAll(
                        задачаЧтения,
                        задачаHeartbeat);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ошибка)
                {
                    if (!токен.IsCancellationRequested &&
                        !остановлен)
                    {
                        ЗаписатьЛог(
                            "Ошибка канала связи: " +
                            ошибка);
                    }
                }

                if (!токен.IsCancellationRequested &&
                    !остановлен)
                {
                    throw new IOException(
                        "Соединение с сервером завершилось.");
                }
            }
            finally
            {
                токенСвязи.Cancel();
            }
        }


        // =========================================================
        // ЧТЕНИЕ
        // =========================================================

        private async Task ЧитатьСообщенияAsync(
            CancellationToken токен)
        {
            StreamReader? локальныйЧитатель;

            lock (блокировкаСостояния)
            {
                локальныйЧитатель =
                    читатель;
            }

            if (локальныйЧитатель == null)
            {
                throw new IOException(
                    "Поток чтения не создан.");
            }

            while (!токен.IsCancellationRequested)
            {
                string? строка;

                try
                {
                    строка =
                        await локальныйЧитатель.ReadLineAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (IOException)
                {
                    throw;
                }

                if (строка == null)
                {
                    throw new IOException(
                        "Сервер закрыл соединение.");
                }

                if (string.IsNullOrWhiteSpace(
                        строка))
                {
                    continue;
                }

                СетевоеСообщение? сообщение;

                try
                {
                    сообщение =
                        JsonSerializer.Deserialize<
                            СетевоеСообщение>(
                                строка);
                }
                catch (JsonException ошибка)
                {
                    ЗаписатьЛог(
                        "Получено некорректное сообщение: " +
                        ошибка);

                    continue;
                }

                if (сообщение == null)
                {
                    continue;
                }

                ЗаписатьЛог(
                    $"Патруль получил: {сообщение.Тип}");

                if (сообщение.Тип ==
                    серьёзный.Сеть.ТипСообщения.HeartbeatОтвет)
                {
                    continue;
                }

                if (сообщение.Тип ==
                    серьёзный.Сеть.ТипСообщения.ПриветствиеОтвет)
                {
                    continue;
                }

                // Личный чат (SQLite)

                if (сообщение.Тип ==
                    серьёзный.Сеть.ТипСообщения.Чат)
                {
                    var msg =
                        сообщение.ПолучитьДанные<ChatMessage>();

                    if (msg != null)
                    {
                        ChatLiveEvents.Raise(msg);
                    }

                    continue;
                }

                var обработчик =
                    ПолученоСообщение;

                if (обработчик != null)
                {
                    await обработчик(
                        сообщение);
                }
            }
        }


        // =========================================================
        // HEARTBEAT
        // =========================================================

        private async Task ОтправлятьHeartbeatAsync(
            CancellationToken токен)
        {
            while (!токен.IsCancellationRequested)
            {
                if (!Подключен)
                {
                    throw new IOException(
                        "Соединение отсутствует.");
                }

                var heartbeat =
                    СетевоеСообщение.Создать(
                        серьёзный.Сеть.ТипСообщения.Heartbeat);

                heartbeat.КомпьютерId =
                    конфигурация.КомпьютерId;

                heartbeat.ИмяКомпьютера =
                    конфигурация.ИмяКомпьютера;

                heartbeat.ИмяWindows =
                    Environment.MachineName;

                heartbeat.Успешно =
                    true;

                var успешно =
                    await ОтправитьAsync(
                        heartbeat);

                if (!успешно)
                {
                    throw new IOException(
                        "Heartbeat не отправлен.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(
                        Math.Max(
                            1,
                            конфигурация
                                .ИнтервалHeartbeatСекунд)),
                    токен);
            }
        }


        // =========================================================
        // ОТПРАВКА
        // =========================================================

        public async Task<bool> ОтправитьAsync(
            СетевоеСообщение сообщение)
        {
            if (сообщение == null)
            {
                return false;
            }

            StreamWriter? локальныйПисатель;

            lock (блокировкаСостояния)
            {
                локальныйПисатель =
                    писатель;
            }

            if (локальныйПисатель == null ||
                !Подключен)
            {
                return false;
            }

            try
            {
                await блокировкаОтправки.WaitAsync();

                try
                {
                    var json =
                        JsonSerializer.Serialize(
                            сообщение);

                    await локальныйПисатель.WriteLineAsync(
                        json);

                    await локальныйПисатель.FlushAsync();

                    return true;
                }
                finally
                {
                    блокировкаОтправки.Release();
                }
            }
            catch (Exception ошибка)
            {
                ЗаписатьЛог(
                    "Ошибка отправки сообщения: " +
                    ошибка);

                return false;
            }
        }


        // =========================================================
        // ЗАКРЫТЬ
        // =========================================================

        public void Закрыть()
        {
            остановлен = true;

            ЗакрытьСоединение();
        }


        private void ЗакрытьСоединение()
        {
            StreamReader? локальныйЧитатель;
            StreamWriter? локальныйПисатель;
            NetworkStream? локальныйПоток;
            TcpClient? локальныйКлиент;

            lock (блокировкаСостояния)
            {
                локальныйЧитатель = читатель;
                локальныйПисатель = писатель;
                локальныйПоток = поток;
                локальныйКлиент = клиент;

                читатель = null;
                писатель = null;
                поток = null;
                клиент = null;
            }

            try
            {
                локальныйЧитатель?.Dispose();
            }
            catch
            {
            }

            try
            {
                локальныйПисатель?.Dispose();
            }
            catch
            {
            }

            try
            {
                локальныйПоток?.Dispose();
            }
            catch
            {
            }

            try
            {
                локальныйКлиент?.Close();
                локальныйКлиент?.Dispose();
            }
            catch
            {
            }
        }


        // =========================================================
        // ЛОГ
        // =========================================================

        private static void ЗаписатьЛог(
            string текст)
        {
            try
            {
                серьёзный.патруль.Сервисы.Лог.Записать(
                    текст);
            }
            catch
            {
            }
        }


        // =========================================================
        // DISPOSE
        // =========================================================

        public void Dispose()
        {
            Закрыть();

            блокировкаОтправки.Dispose();
        }
    }
}