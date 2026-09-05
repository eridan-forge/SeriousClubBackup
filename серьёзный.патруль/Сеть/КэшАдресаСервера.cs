using System;
using System.IO;
using System.Text.Json;

namespace серьёзный.Патруль.Сеть
{
    public class АдресСервераКэш
    {
        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }
    }

    public static class КэшАдресаСервера
    {
        private static readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(
                     Environment.SpecialFolder.LocalApplicationData),
                "SeriousClubPatrol",
                "server-cache.json");

        public static (string Ip, int Port)? Загрузить()
        {
            try
            {
                if (!File.Exists(путь))
                    return null;

                var данные =
                    JsonSerializer.Deserialize<АдресСервераКэш>(
                        File.ReadAllText(путь));

                if (данные == null ||
                    string.IsNullOrWhiteSpace(данные.Ip) ||
                    данные.Port <= 0)
                    return null;

                return (данные.Ip, данные.Port);
            }
            catch
            {
                return null;
            }
        }

        public static void Сохранить(string ip, int port)
        {
            try
            {
                Directory.CreateDirectory(
                     Path.GetDirectoryName(путь)!);
                var данные =
                    new АдресСервераКэш
                    {
                        Ip = ip,
                        Port = port
                    };

                File.WriteAllText(
                    путь,
                    JsonSerializer.Serialize(данные));
            }
            catch
            {
            }
        }
    }
}