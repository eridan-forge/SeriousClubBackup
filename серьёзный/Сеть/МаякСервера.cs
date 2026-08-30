using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace серьёзный.Сеть
{
    public class МаякСервера
    {
        private readonly CancellationTokenSource токен = new();

        public void Запустить()
        {
            _ = Task.Run(async () =>
            {
                using var udp = new UdpClient();
                udp.EnableBroadcast = true;

                var адрес = new IPEndPoint(IPAddress.Broadcast, 47820);

                while (!токен.Token.IsCancellationRequested)
                {
                    try
                    {
                        var ip = Dns.GetHostAddresses(Dns.GetHostName())
                            .First(x => x.AddressFamily == AddressFamily.InterNetwork)
                            .ToString();

                        var данные = Encoding.UTF8.GetBytes($"SERIOUS_SERVER|{ip}|47821");

                        await udp.SendAsync(данные, данные.Length, адрес);
                        await Task.Delay(1000, токен.Token);
                    }
                    catch
                    {
                    }
                }
            });
        }

        public void Остановить()
        {
            токен.Cancel();
        }
    }
}