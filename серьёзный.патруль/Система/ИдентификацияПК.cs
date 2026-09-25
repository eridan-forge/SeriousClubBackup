using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace серьёзный.Патруль.Система
{
    public static class ИдентификацияПК
    {
        public static string ПолучитьИмяWindows()
        {
            return Environment.MachineName;
        }

        public static string ПолучитьIP()
        {
            try
            {
                var интерфейс = ПолучитьОсновнойИнтерфейс();

                if (интерфейс == null)
                    return string.Empty;

                var адрес =
                    интерфейс.GetIPProperties()
                        .UnicastAddresses
                        .FirstOrDefault(x =>
                            x.Address.AddressFamily ==
                            AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(x.Address));

                return адрес?.Address.ToString()
                       ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string ПолучитьMAC()
        {
            try
            {
                var интерфейс = ПолучитьОсновнойИнтерфейс();

                if (интерфейс == null)
                    return string.Empty;

                return string.Join(
                    ":",
                    интерфейс
                        .GetPhysicalAddress()
                        .GetAddressBytes()
                        .Select(x => x.ToString("X2")));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static NetworkInterface? ПолучитьОсновнойИнтерфейс()
        {
            try
            {
                return NetworkInterface
                    .GetAllNetworkInterfaces()

                    // Только работающие адаптеры
                    .Where(x =>
                        x.OperationalStatus ==
                        OperationalStatus.Up)

                    // Только настоящий Ethernet
                    .Where(x =>
                        x.NetworkInterfaceType ==
                        NetworkInterfaceType.Ethernet ||
                        x.NetworkInterfaceType ==
                        NetworkInterfaceType.GigabitEthernet)

                    // Должен иметь IPv4
                    .Where(x =>
                        x.GetIPProperties()
                            .UnicastAddresses
                            .Any(a =>
                                a.Address.AddressFamily ==
                                AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(a.Address)))

                    // Должен иметь IPv4-шлюз.
                    // Это отсекает VirtualBox Host-Only,
                    // TAP и другие локальные виртуальные адаптеры.
                    .Where(x =>
                        x.GetIPProperties()
                            .GatewayAddresses
                            .Any(g =>
                                g.Address.AddressFamily ==
                                AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(g.Address)))

                    // Берём первый подходящий настоящий Ethernet
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}