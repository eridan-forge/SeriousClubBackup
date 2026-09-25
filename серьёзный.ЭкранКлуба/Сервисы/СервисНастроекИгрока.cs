using System;
using System.IO;
using System.Text.Json;
using серьёзный.ЭкранКлуба.Модели;

namespace серьёзный.ЭкранКлуба.Сервисы
{
    public class СервисНастроекИгрока
    {
        private readonly string папка;

        public СервисНастроекИгрока()
        {
            папка = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "PlayerSettings");

            Directory.CreateDirectory(папка);
        }

        private string Путь(Guid id) =>
            Path.Combine(папка, $"{id}.json");

        public НастройкиИгрока Загрузить(Guid id)
        {
            var путь = Путь(id);

            if (!File.Exists(путь))
                return new НастройкиИгрока { АккаунтId = id };

            try
            {
                return JsonSerializer.Deserialize<НастройкиИгрока>(
                           File.ReadAllText(путь))
                       ?? new НастройкиИгрока { АккаунтId = id };
            }
            catch
            {
                return new НастройкиИгрока { АккаунтId = id };
            }
        }

        public void Сохранить(НастройкиИгрока настройки)
        {
            File.WriteAllText(
                Путь(настройки.АккаунтId),
                JsonSerializer.Serialize(
                    настройки,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }
    }
}