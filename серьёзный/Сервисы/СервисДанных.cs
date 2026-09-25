using System;
using System.IO;
using System.Text.Json;

namespace серьёзный.Сервисы
{
    public class СервисДанных
    {
        private readonly string папкаДанных;
        private readonly JsonSerializerOptions настройкиJson;

        public СервисДанных()
        {
            папкаДанных = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "Серьёзный");

            Directory.CreateDirectory(папкаДанных);

            настройкиJson = new JsonSerializerOptions
            {
                WriteIndented = true
            };
        }

        public void Сохранить<T>(
            string имяФайла,
            T данные)
        {
            var путь = ПолучитьПуть(имяФайла);

            var json = JsonSerializer.Serialize(
                данные,
                настройкиJson);

            File.WriteAllText(путь, json);
        }

        public T? Загрузить<T>(
            string имяФайла)
        {
            var путь = ПолучитьПуть(имяФайла);

            if (!File.Exists(путь))
                return default;

            var json = File.ReadAllText(путь);

            return JsonSerializer.Deserialize<T>(
                json,
                настройкиJson);
        }

        public bool Существует(
            string имяФайла)
        {
            return File.Exists(
                ПолучитьПуть(имяФайла));
        }

        private string ПолучитьПуть(
            string имяФайла)
        {
            return Path.Combine(
                папкаДанных,
                имяФайла);
        }
    }
}