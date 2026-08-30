using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

                var адрес =
                    new IPEndPoint(
                        IPAddress.Broadcast,
                        47820);

                while (!токен.Token.IsCancellationRequested)
                {
                    try
                    {
                        var ip = ПолучитьЛокальныйIP();

                        if (!string.IsNullOrEmpty(ip))
                        {
                            var данные =
                                Encoding.UTF8.GetBytes(
                                    $"SERIOUS_SERVER|{ip}|47821");

                            await udp.SendAsync(
                                данные,
                                данные.Length,
                                адрес);
                        }

                        await Task.Delay(
                            1000,
                            токен.Token);
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

        private static string ПолучитьЛокальныйIP()
        {
            try
            {
                var интерфейс =
                    NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(x =>
                            x.OperationalStatus ==
                            OperationalStatus.Up)
                        .Where(x =>
                            x.NetworkInterfaceType ==
                            NetworkInterfaceType.Ethernet ||
                            x.NetworkInterfaceType ==
                            NetworkInterfaceType.GigabitEthernet ||
                            x.NetworkInterfaceType ==
                            NetworkInterfaceType.Wireless80211)
                        .Where(x =>
                            x.GetIPProperties()
                                .GatewayAddresses
                                .Any(g =>
                                    g.Address.AddressFamily ==
                                    AddressFamily.InterNetwork &&
                                    !IPAddress.IsLoopback(g.Address)))
                        .FirstOrDefault();

                if (интерфейс == null)
                    return string.Empty;

                var локальный =
                    интерфейс.GetIPProperties()
                        .UnicastAddresses
                        .FirstOrDefault(x =>
                            x.Address.AddressFamily ==
                            AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(x.Address));

                return локальный?.Address.ToString()
                       ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}