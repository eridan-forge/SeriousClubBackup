using System.Text.Json;

namespace серьёзный.Сеть
{
    public class СетевоеСообщение
    {
        public string Версия { get; set; } = "1.0";

        public ТипСообщения Тип { get; set; }

        public string ИдентификаторСообщения { get; set; } = string.Empty;

        public int? КомпьютерId { get; set; }

        public string? ИмяКомпьютера { get; set; }

        public string? ИмяWindows { get; set; }

        // Здесь будет храниться любой сериализованный объект:
        // КомандаПатрулю, СообщениеЧата, DirectMessage и т.д.
        public string? Данные { get; set; }

        public bool Успешно { get; set; }

        public string? Ошибка { get; set; }

        public static СетевоеСообщение Создать(
            ТипСообщения тип)
        {
            return new СетевоеСообщение
            {
                Тип = тип,
                ИдентификаторСообщения =
                    Guid.NewGuid().ToString("N")
            };
        }

        public T? ПолучитьДанные<T>()
        {
            if (string.IsNullOrWhiteSpace(Данные))
                return default;

            return JsonSerializer.Deserialize<T>(Данные);
        }

        public void УстановитьДанные<T>(T данные)
        {
            Данные = JsonSerializer.Serialize(данные);
        }
    }
}