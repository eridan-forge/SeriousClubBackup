using System;
using System.IO;
using System.Text.Json;

namespace серьёзный.Патруль.Система
{
    public class КонфигурацияПатрульJson
    {
        public int КомпьютерId { get; set; }

        public string? Имя { get; set; }

        public string? СерверIP { get; set; }

        public int? СерверПорт { get; set; }
    }


    public static class КонфигурацияПатруляЗагрузчик
    {
        private static readonly JsonSerializerOptions Опции =
            new()
            {
                PropertyNameCaseInsensitive = true
            };


        private static string Путь =>
            Path.Combine(
                AppContext.BaseDirectory,
                "patrol.json");


        public static КонфигурацияПатрульJson Загрузить()
        {
            if (!File.Exists(Путь))
            {
                throw new FileNotFoundException(
                    "Не найден файл patrol.json.",
                    Путь);
            }

            string текст;

            try
            {
                текст =
                    File.ReadAllText(Путь);
            }
            catch (Exception ошибка)
            {
                throw new IOException(
                    "Не удалось прочитать patrol.json.",
                    ошибка);
            }

            if (string.IsNullOrWhiteSpace(текст))
            {
                throw new InvalidOperationException(
                    "Файл patrol.json пуст.");
            }

            КонфигурацияПатрульJson? конфигурация;

            try
            {
                конфигурация =
                    JsonSerializer.Deserialize<
                        КонфигурацияПатрульJson>(
                            текст,
                            Опции);
            }
            catch (JsonException ошибка)
            {
                throw new InvalidOperationException(
                    "Файл patrol.json содержит некорректный JSON.",
                    ошибка);
            }

            if (конфигурация == null)
            {
                throw new InvalidOperationException(
                    "Не удалось загрузить patrol.json.");
            }

            if (конфигурация.КомпьютерId <= 0)
            {
                throw new InvalidOperationException(
                    "В patrol.json указан некорректный КомпьютерId.");
            }

            if (конфигурация.СерверПорт.HasValue &&
                (конфигурация.СерверПорт.Value <= 0 ||
                 конфигурация.СерверПорт.Value > 65535))
            {
                throw new InvalidOperationException(
                    "В patrol.json указан некорректный СерверПорт.");
            }

            return конфигурация;
        }
    }
}