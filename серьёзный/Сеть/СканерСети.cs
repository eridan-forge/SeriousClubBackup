using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace серьёзный.Сеть
{
    public class НайденныйКомпьютер
    {
        public string IP { get; set; } = "";
        public string Имя { get; set; } = "";
        public string MAC { get; set; } = "";
        public bool Онлайн { get; set; }
    }

    public static class СканерСети
    {
        public static async Task<List<НайденныйКомпьютер>> СканироватьAsync()
        {
            var лимит = new SemaphoreSlim(40);

            var задачи = Enumerable.Range(1, 254).Select(async i =>
            {
                await лимит.WaitAsync();
                try
                {
                    using var ping = new Ping();
                    await ping.SendPingAsync($"192.168.0.{i}", 30);
                }
                catch { }
                finally
                {
                    лимит.Release();
                }
            });

            await Task.WhenAll(задачи);

            await Task.Delay(100);

            var p = new Process();

            p.StartInfo.FileName = "arp";
            p.StartInfo.Arguments = "-a";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            string текст = p.StandardOutput.ReadToEnd();

            p.WaitForExit();

            var список = new List<НайденныйКомпьютер>();

            var совпадения = Regex.Matches(
                текст,
                @"192\.168\.0\.\d+\s+[0-9a-fA-F\-]{17}");

            foreach (Match m in совпадения)
            {
                var части = Regex.Split(
                    m.Value.Trim(),
                    @"\s+");

                string ip = части[0];
                string mac = части[1].ToUpper();

                bool онлайн = false;
                string имя = "";

                try
                {
                    using var ping = new Ping();

                    онлайн = (await ping.SendPingAsync(ip, 40)).Status ==
                        IPStatus.Success;

                    if (онлайн)
                    {
                        try
                        {
                            имя = (await Dns.GetHostEntryAsync(ip)).HostName;
                        }
                        catch { }
                    }
                }
                catch { }

                список.Add(new НайденныйКомпьютер
                {
                    IP = ip,
                    MAC = mac,
                    Имя = имя,
                    Онлайн = онлайн
                });
            }

            var свой = NetworkInterface
    .GetAllNetworkInterfaces()
    .First(x =>
        x.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
        x.OperationalStatus == OperationalStatus.Up);

            var ipv4 = свой
                .GetIPProperties()
                .UnicastAddresses
                .First(x => x.Address.AddressFamily ==
                    System.Net.Sockets.AddressFamily.InterNetwork);

            var мойIP = ipv4.Address.ToString();

            var мойMAC = string.Join("-",
                свой.GetPhysicalAddress()
                    .GetAddressBytes()
                    .Select(x => x.ToString("X2")));

            if (!список.Any(x => x.IP == мойIP))
            {
                список.Add(new НайденныйКомпьютер
                {
                    IP = мойIP,
                    MAC = мойMAC,
                    Имя = Environment.MachineName,
                    Онлайн = true
                });
            }

            return список
                .GroupBy(x => x.IP)
                .Select(x => x.First())
                .OrderBy(x => IPAddress.Parse(x.IP).GetAddressBytes()[3])
                .ToList();
        }
    }
}