using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace серьёзный.Сеть
{
    public static class СервисWakeOnLan
    {
        public static async Task<bool> ВключитьAsync(string mac)
        {
            try
            {
                byte[] macBytes = PhysicalAddress
                    .Parse(mac.Replace("-", "").Replace(":", ""))
                    .GetAddressBytes();

                byte[] packet = new byte[102];

                for (int i = 0; i < 6; i++)
                    packet[i] = 0xFF;

                for (int i = 0; i < 16; i++)
                    Buffer.BlockCopy(macBytes, 0, packet, 6 + i * 6, 6);

                using var udp = new UdpClient();
                udp.EnableBroadcast = true;

                var интерфейсы = NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(x =>
                        x.OperationalStatus == OperationalStatus.Up &&
                        x.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var nic in интерфейсы)
                {
                    foreach (var адрес in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (адрес.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        if (адрес.IPv4Mask == null)
                            continue;

                        var broadcast = ПолучитьBroadcast(
                            адрес.Address,
                            адрес.IPv4Mask);

                        for (int i = 0; i < 3; i++)
                        {
                            await udp.SendAsync(
                                packet,
                                packet.Length,
                                new IPEndPoint(broadcast, 9));

                            await Task.Delay(20);
                        }
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    await udp.SendAsync(
                        packet,
                        packet.Length,
                        new IPEndPoint(IPAddress.Broadcast, 9));

                    await Task.Delay(20);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IPAddress ПолучитьBroadcast(
            IPAddress ip,
            IPAddress mask)
        {
            var ipBytes = ip.GetAddressBytes();
            var maskBytes = mask.GetAddressBytes();

            var broadcast = new byte[4];

            for (int i = 0; i < 4; i++)
                broadcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

            return new IPAddress(broadcast);
        }
    }
}