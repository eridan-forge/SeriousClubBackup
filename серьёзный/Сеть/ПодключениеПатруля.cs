using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreComputers;

namespace серьёзный.Сеть
{
    public class ПодключениеПатруля
    {
        private readonly TcpClient клиент;

        private readonly NetworkStream поток;

        private readonly StreamReader читатель;

        private readonly StreamWriter писатель;

        private readonly SemaphoreSlim
            блокировкаОтправки = new(1, 1);

        public int КомпьютерId { get; }

        public string ИмяКомпьютера { get; }

        public string IPАдрес { get; }

        public DateTime ПоследнийСигнал { get; set; } =
    DateTime.Now;

        public DateTime ПоследнийHeartbeat
        {
            get;
            private set;
        }

        public bool Подключено =>
            клиент.Connected;

        public event Action<ПодключениеПатруля, СетевоеСообщение>? ПолученоСообщение;

        public event Action<ПодключениеПатруля>? Отключено;

        private ПодключениеПатруля(
            TcpClient клиент,
            int компьютерId,
            string имяКомпьютера)
        {
            this.клиент = клиент;

            поток = клиент.GetStream();

            читатель = new StreamReader(
                поток,
                new UTF8Encoding(false));

            писатель = new StreamWriter(
                поток,
                new UTF8Encoding(false))
            {
                AutoFlush = true
            };

            КомпьютерId = компьютерId;

            ИмяКомпьютера = имяКомпьютера;

            IPАдрес =
                клиент.Client.RemoteEndPoint?
                    .ToString() ?? string.Empty;

            ПоследнийHeartbeat =
                DateTime.Now;
        }

        public static async Task<ПодключениеПатруля?>
            СоздатьПослеHandshakeAsync(
                TcpClient клиент,
                CancellationToken токен)
        {
            using var таймер =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(10));

            using var связанныйТокен =
                CancellationTokenSource.CreateLinkedTokenSource(
                    токен,
                    таймер.Token);

            var поток = клиент.GetStream();

            using var читатель =
                new StreamReader(
                    поток,
                    new UTF8Encoding(false),
                    leaveOpen: true);

            using var писатель =
                new StreamWriter(
                    поток,
                    new UTF8Encoding(false),
                    leaveOpen: true)
                {
                    AutoFlush = true
                };

            var строка =
                await читатель.ReadLineAsync(
                    связанныйТокен.Token);

            if (string.IsNullOrWhiteSpace(строка))
                return null;

            var приветствие =
                JsonSerializer.Deserialize<СетевоеСообщение>(строка);

            if (приветствие == null ||
                приветствие.Тип !=
                ТипСообщения.Приветствие)
            {
                return null;
            }

            var данные =
                приветствие
                    .ПолучитьДанные<ДанныеHandshake>();

            if (данные == null)
                return null;

            if (данные.КомпьютерId <= 0 ||
                string.IsNullOrWhiteSpace(
                    данные.ИмяКомпьютера))
            {
                return null;
            }

            var ip =
    (клиент.Client.RemoteEndPoint as IPEndPoint)?
        .Address.ToString() ?? "";

            try
            {
                КартаКомпьютеров.ЗарегистрироватьИлиОбновить(
                    данные.КомпьютерId,
                    данные.ИмяКомпьютера,
                    данные.ИмяWindows,
                    ip,
                    данные.МАСАдрес);
            }
            catch
            {
                // Регистрация не должна ронять handshake.
            }

            var ответ = СетевоеСообщение.Создать(
    ТипСообщения.ПриветствиеОтвет);

            

            ответ.КомпьютерId =
                данные.КомпьютерId;

            ответ.Успешно = true;

            await писатель.WriteLineAsync(
                JsonSerializer.Serialize(ответ));

            return new ПодключениеПатруля(
                клиент,
                данные.КомпьютерId,
                данные.ИмяКомпьютера);
        }

        public async Task ЗапуститьAsync(
            CancellationToken токен)
        {
            try
            {
                while (!токен.IsCancellationRequested)
                {
                    var строка =
                        await читатель.ReadLineAsync();

                    if (строка == null)
                        break;

                    if (string.IsNullOrWhiteSpace(
                        строка))
                    {
                        continue;
                    }

                    var сообщение =
                        JsonSerializer.Deserialize<СетевоеСообщение>(
                            строка);

                    if (сообщение == null)
                        continue;

                    if (сообщение.Тип ==
                        ТипСообщения.Heartbeat)
                    {
                        ПоследнийHeartbeat =
                            DateTime.Now;

                        var ответ =
                            СетевоеСообщение.Создать(
                                ТипСообщения.HeartbeatОтвет);

                        ответ.КомпьютерId =
                            КомпьютерId;

                        ответ.Успешно = true;

                        await ОтправитьAsync(ответ);
                    }

                    ПолученоСообщение?.Invoke(
                        this,
                        сообщение);
                }
            }
            catch
            {
            }
            finally
            {
                Отключено?.Invoke(this);

                Закрыть();
            }
        }

        public async Task<bool>
            ОтправитьAsync(
                СетевоеСообщение сообщение)
        {
            if (!Подключено)
                return false;

            try
            {
                await блокировкаОтправки.WaitAsync();

                try
                {
                    var json =
                        JsonSerializer.Serialize(
                            сообщение);

                    await писатель.WriteLineAsync(
                        json);

                    return true;
                }
                finally
                {
                    блокировкаОтправки.Release();
                }
            }
            catch
            {
                return false;
            }
        }

        public void Закрыть()
        {
            try
            {
                читатель.Dispose();
                писатель.Dispose();
                поток.Dispose();
                клиент.Close();
            }
            catch
            {
            }
        }
    }

    public class ДанныеHandshake
    {
        public int КомпьютерId { get; set; }

        public string ИмяКомпьютера
        {
            get;
            set;
        } = string.Empty;

        public string ИмяWindows
        {
            get;
            set;
        } = string.Empty;

        public string МАСАдрес
        {
            get;
            set;
        } = string.Empty;

        public string ВерсияПатруля
        {
            get;
            set;
        } = string.Empty;
    }
}