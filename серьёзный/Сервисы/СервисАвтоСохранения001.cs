using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class СервисАвтоСохранения001
    {
        private readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "snapshot001.json");

        public void Сохранить(
            IReadOnlyList<Сеанс> активные)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(путь)!);

            var снимок =
                new СнимокСеансов001
                {
                    ВремяСохранения =
                        DateTime.Now,

                    Сеансы =
                        активные
                            .Select(x =>
                                new Сеанс001
                                {
                                    Id = x.Id,
                                    КомпьютерId = x.КомпьютерId,
                                    ИмяКлиента = x.ИмяКлиента,
                                    АккаунтGuid = x.АккаунтGuid,
                                    КупленноеВремя = x.КупленноеВремя,
                                    ВремяАккаунта = x.ВремяАккаунта,
                                    ИспользуетсяОстатокАккаунта =
                                        x.ИспользуетсяОстатокАккаунта,
                                    Начало = x.Начало,
                                    ЗапланированноеОкончание =
                                        x.ЗапланированноеОкончание,
                                    Стоимость = x.Стоимость
                                })
                            .ToList()
                };

            File.WriteAllText(
                путь,
                JsonSerializer.Serialize(
                    снимок,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }

        public СнимокСеансов001? Загрузить()
        {
            if (!File.Exists(путь))
                return null;

            var json = File.ReadAllText(путь);

            if (string.IsNullOrWhiteSpace(json))
                return null;

            // повреждённый файл
            if (json[0] == '\0')
                return null;

            try
            {
                return JsonSerializer.Deserialize<СнимокСеансов001>(json);
            }
            catch
            {
                return null;
            }
        }

        public void Очистить()
        {
            if (File.Exists(путь))
                File.Delete(путь);
        }
    }
}