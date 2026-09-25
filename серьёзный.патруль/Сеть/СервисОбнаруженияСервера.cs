using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace серьёзный.Патруль.Сеть
{
    public static class СервисОбнаруженияСервера
    {
        private const int ПортМаяка = 47820;

        public static async Task<(string Ip, int Port)?> НайтиСерверAsync(
            TimeSpan таймаут,
            CancellationToken токен)
        {
            using var udp = new UdpClient();

            try
            {
                udp.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true);

                udp.Client.Bind(
                    new IPEndPoint(IPAddress.Any, ПортМаяка));
            }
            catch
            {
                // Порт занят другим процессом — в этот раз
                // просто не услышим маяк, не критично.
                return null;
            }

            using var связанныйТокен =
                CancellationTokenSource.CreateLinkedTokenSource(токен);

            связанныйТокен.CancelAfter(таймаут);

            try
            {
                while (!связанныйТокен.IsCancellationRequested)
                {
                    var результат =
                        await udp.ReceiveAsync(связанныйТокен.Token);

                    var текст =
                        Encoding.UTF8.GetString(результат.Buffer);

                    var части = текст.Split('|');

                    if (части.Length != 3 ||
                        части[0] != "SERIOUS_SERVER")
                        continue;

                    if (!int.TryParse(части[2], out var порт))
                        continue;

                    return (части[1], порт);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
            }

            return null;
        }
    }
}