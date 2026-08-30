using System;
using System.Collections.Generic;
using System.Linq;

namespace серьёзный.Патруль.Система
{
    public static class КартаКомпьютеров
    {

        private static readonly List<ЗаписьПК>
       компьютеры =
       new()
       {
        new ЗаписьПК(
            1,
            "PC-01",
            "DESKTOP-IN5G5T1",
            "192.168.31.197",
            "34:5A:60:F4:E5:29"),

        new ЗаписьПК(
            2,
            "PC-02",
            "DESKTOP-E079RMC",
            "192.168.31.55",
            "FC:9D:05:66:31:35"),

        new ЗаписьПК(
            3,
            "PC-03",
            "DESKTOP-BOAJUJV",
            "192.168.31.150",
            "34:5A:60:F4:E5:F4"),

        new ЗаписьПК(
            4,
            "PC-04",
            "DESKTOP-5S1UI1G",
            "192.168.31.204",
            "34:5A:60:F4:E5:30"),

        new ЗаписьПК(
            5,
            "PC-05",
            "DESKTOP-TB208IO",
            "192.168.31.147",
            "34:5A:60:F4:E5:F1"),

        // ПК брата
        new ЗаписьПК(
            100,
            "TEST-01",
            "DESKTOP-5P441FK",
            "192.168.0.237",
            "10-FF-E0-4C-98-9C")
       };

        public static IReadOnlyList<ЗаписьПК> Все =>
            компьютеры;

        public static ЗаписьПК? НайтиПоId(int id)
        {
            return компьютеры.FirstOrDefault(
                x => x.Id == id);
        }

        public static ЗаписьПК? НайтиПоIP(string ip)
        {
            return компьютеры.FirstOrDefault(
                x => string.Equals(
                    x.IP,
                    ip,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static ЗаписьПК? НайтиПоMAC(string mac)
        {
            var нормализованныйMAC =
                Нормализовать(mac);

            return компьютеры.FirstOrDefault(
                x => Нормализовать(x.MAC) ==
                     нормализованныйMAC);
        }

        public static ЗаписьПК? НайтиПоИмениWindows(
            string имяWindows)
        {
            return компьютеры.FirstOrDefault(
                x => string.Equals(
                    x.ИмяWindows,
                    имяWindows,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static ЗаписьПК?
            НайтиТекущийКомпьютер()
        {
            var имя =
                ИдентификацияПК.ПолучитьИмяWindows();

            var ip =
                ИдентификацияПК.ПолучитьIP();

            var mac =
                ИдентификацияПК.ПолучитьMAC();

            var поMAC =
                НайтиПоMAC(mac);

            if (поMAC != null)
            {
                if (string.Equals(
                        поMAC.ИмяWindows,
                        имя,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return поMAC;
                }
            }

            var поИмени =
                НайтиПоИмениWindows(имя);

            if (поИмени != null &&
                string.Equals(
                    поИмени.IP,
                    ip,
                    StringComparison.OrdinalIgnoreCase))
            {
                return поИмени;
            }

            return null;
        }

        private static string Нормализовать(
            string? значение)
        {
            if (string.IsNullOrWhiteSpace(значение))
                return string.Empty;

            return значение
                .Replace(":", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim()
                .ToUpperInvariant();
        }
    }

    public class ЗаписьПК
    {
        public int Id { get; }

        public string Имя { get; }

        public string ИмяWindows { get; }

        public string IP { get; }

        public string MAC { get; }

        public ЗаписьПК(
            int id,
            string имя,
            string имяWindows,
            string ip,
            string mac)
        {
            Id = id;
            Имя = имя;
            ИмяWindows = имяWindows;
            IP = ip;
            MAC = mac;
        }
    }
}