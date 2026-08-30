using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace серьёзный.Сеть
{
    public class СерверСвязи
    {
        private readonly TcpListener сервер;

        private readonly ConcurrentDictionary<int, ПодключениеПатруля> подключения =
            new();

        private CancellationTokenSource? токенИсточника;

        private readonly object блокировкаЗапуска =
            new();

        public bool Запущен
        {
            get;
            private set;
        }

        // =========================================================
        // СОБЫТИЯ
        // =========================================================

        public event Action<ПодключениеПатруля>? ПатрульПодключился;

        public event Action<ПодключениеПатруля>? ПатрульОтключился;

        public event Action<ПодключениеПатруля, СетевоеСообщение>? ПолученоСообщение;

        // =========================================================
        // КОНСТРУКТОР
        // =========================================================

        public СерверСвязи(int порт)
        {
            if (порт <= 0 || порт > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(порт),
                    "Порт должен находиться в диапазоне 1-65535.");
            }

            сервер =
                new TcpListener(IPAddress.Any, порт);
        }

        // =========================================================
        // ЗАПУСК
        // =========================================================

        public async Task ЗапуститьAsync(CancellationToken внешнийТокен)
        {
            lock (блокировкаЗапуска)
            {
                if (Запущен)
                    return;

                токенИсточника =
                    CancellationTokenSource.CreateLinkedTokenSource(внешнийТокен);

                сервер.Start();

                Запущен = true;
            }

            var токен =
                токенИсточника.Token;

            try
            {
                while (!токен.IsCancellationRequested)
                {
                    TcpClient клиент;

                    try
                    {
                        клиент =
                            await сервер.AcceptTcpClientAsync(токен);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        if (токен.IsCancellationRequested)
                            break;

                        continue;
                    }

                    _ = Task.Run(
                        () => ПринятьПатрульAsync(клиент, токен),
                        CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                ЗавершитьРаботу();
            }
        }

        // =========================================================
        // ПРИЁМ ПАТРУЛЯ
        // =========================================================

        private async Task ПринятьПатрульAsync(
            TcpClient клиент,
            CancellationToken токен)
        {
            ПодключениеПатруля? новоеПодключение =
                null;

            try
            {
                клиент.NoDelay = true;

                новоеПодключение =
                    await ПодключениеПатруля.СоздатьПослеHandshakeAsync(
                        клиент,
                        токен);

                if (новоеПодключение == null)
                {
                    клиент.Close();
                    return;
                }

                int компьютерId =
                    новоеПодключение.КомпьютерId;

                if (подключения.TryGetValue(
                        компьютерId,
                        out var старое))
                {
                    ((ICollection<KeyValuePair<int, ПодключениеПатруля>>)подключения)
                        .Remove(new KeyValuePair<int, ПодключениеПатруля>(компьютерId, старое));

                    try
                    {
                        старое.Закрыть();
                    }
                    catch
                    {
                    }
                }

                подключения[компьютерId] =
                    новоеПодключение;

                новоеПодключение.ПолученоСообщение += ОбработатьСообщение;
                новоеПодключение.Отключено += ОбработатьОтключение;

                ПатрульПодключился?.Invoke(новоеПодключение);

                await новоеПодключение.ЗапуститьAsync(токен);
            }
            catch
            {
                try
                {
                    новоеПодключение?.Закрыть();
                }
                catch
                {
                }

                try
                {
                    клиент.Close();
                }
                catch
                {
                }
            }
        }

        // =========================================================
        // МАРШРУТИЗАЦИЯ СООБЩЕНИЙ
        // =========================================================

        private async void ОбработатьСообщение(
            ПодключениеПатруля источник,
            СетевоеСообщение сообщение)
        {
            try
            {
                // -------------------------
                // Новый личный чат
                // -------------------------
                if (сообщение.Тип == ТипСообщения.DirectMessage)
                {
                    if (сообщение.КомпьютерId.HasValue &&
                        подключения.TryGetValue(
                            сообщение.КомпьютерId.Value,
                            out var получатель))
                    {
                        await получатель.ОтправитьAsync(сообщение);
                    }
                }

                // Старые события остаются
                ПолученоСообщение?.Invoke(
                    источник,
                    сообщение);
            }
            catch
            {
            }
        }

        // =========================================================
        // ОТКЛЮЧЕНИЕ
        // =========================================================

        private void ОбработатьОтключение(
            ПодключениеПатруля отключившееся)
        {
            if (!подключения.TryGetValue(
                    отключившееся.КомпьютерId,
                    out var текущее))
            {
                return;
            }

            if (!ReferenceEquals(текущее, отключившееся))
            {
                return;
            }

            ((ICollection<KeyValuePair<int, ПодключениеПатруля>>)подключения)
                .Remove(new KeyValuePair<int, ПодключениеПатруля>(
                    отключившееся.КомпьютерId,
                    отключившееся));

            try
            {
                ПатрульОтключился?.Invoke(отключившееся);
            }
            catch
            {
            }
        }

        // =========================================================
        // ПОИСК
        // =========================================================

        public bool ЕстьПодключение(int компьютерId)
        {
            return подключения.ContainsKey(компьютерId);
        }

        public ПодключениеПатруля? ПолучитьПодключение(int компьютерId)
        {
            подключения.TryGetValue(компьютерId, out var пк);

            return пк;
        }

        public int КоличествоПодключений =>
            подключения.Count;

        // =========================================================
        // ОСТАНОВКА
        // =========================================================

        public void Остановить()
        {
            CancellationTokenSource? источник;

            lock (блокировкаЗапуска)
            {
                источник = токенИсточника;

                токенИсточника = null;

                Запущен = false;
            }

            try
            {
                источник?.Cancel();
            }
            catch
            {
            }

            try
            {
                сервер.Stop();
            }
            catch
            {
            }

            ЗавершитьПодключения();

            try
            {
                источник?.Dispose();
            }
            catch
            {
            }
        }

        // =========================================================
        // ЗАВЕРШЕНИЕ
        // =========================================================

        private void ЗавершитьРаботу()
        {
            CancellationTokenSource? источник;

            lock (блокировкаЗапуска)
            {
                источник = токенИсточника;

                токенИсточника = null;

                Запущен = false;
            }

            try
            {
                сервер.Stop();
            }
            catch
            {
            }

            ЗавершитьПодключения();

            try
            {
                источник?.Dispose();
            }
            catch
            {
            }
        }

        private void ЗавершитьПодключения()
        {
            var список =
                подключения.Values.ToList();

            foreach (var пк in список)
            {
                try
                {
                    пк.Закрыть();
                }
                catch
                {
                }
            }

            подключения.Clear();
        }
    }
}