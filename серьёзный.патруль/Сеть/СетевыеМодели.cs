using System;
using System.Text.Json;

namespace серьёзный.Патруль.Сеть
{
    public enum ТипСообщения
    {
        Приветствие = 1,
        ПриветствиеОтвет = 2,
        Heartbeat = 3,
        Команда = 4,
        ОтветНаКоманду = 5,
        Состояние = 6
    }

    public class СетевоеСообщениеПатруля
    {
        public string Версия { get; set; } = "1.0";

        public ТипСообщения Тип { get; set; }

        public string ИдентификаторСообщения { get; set; }
            = Guid.NewGuid().ToString();

        public int? КомпьютерId { get; set; }

        public string? ИмяКомпьютера { get; set; }

        public string? ИмяWindows { get; set; }

        public string? Данные { get; set; }

        public bool Успешно { get; set; }

        public string? Ошибка { get; set; }

        

        public void УстановитьДанные<T>(
            T данные)
        {
            Данные = JsonSerializer.Serialize(данные);
        }

        public T? ПолучитьДанные<T>()
        {
            if (string.IsNullOrWhiteSpace(Данные))
                return default;

            return JsonSerializer.Deserialize<T>(
                Данные);
        }
    }

    public class ДанныеHandshake
    {
        public int КомпьютерId { get; set; }

        public string ИмяКомпьютера { get; set; }
            = string.Empty;

        public string ИмяWindows { get; set; }
            = string.Empty;

        public string ВерсияПатруля { get; set; }
            = "1.0";
    }

    public class КомандаПатрулю
    {
        public серьёзный.Сеть.КомандаПК Команда { get; set; }

        public int? СеансId { get; set; }

        public string? Текст { get; set; }

        public string? Файл { get; set; }
    }
}