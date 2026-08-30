using System;

namespace серьёзный.Модели
{
    public class ИгровойКомпьютер
    {
        public int Id { get; set; }

        public string Имя { get; set; } = string.Empty;

        public string IPАдрес { get; set; } = string.Empty;

        public string MACАдрес { get; set; } = string.Empty;

        public string ИмяWindows { get; set; } = string.Empty;

        public bool Включен { get; set; }

        public bool НаСвязи { get; set; }

        public DateTime ПоследнийКонтакт { get; set; }

        public СостояниеКомпьютера Состояние { get; set; }

        public int? ТекущийСеансId { get; set; }

        public ИгровойКомпьютер()
        {
            Состояние = СостояниеКомпьютера.Неизвестно;
            ПоследнийКонтакт = DateTime.MinValue;
        }
    }

    public enum СостояниеКомпьютера
    {
        Неизвестно,
        Оффлайн,
        Свободен,
        Занят,
        Заблокирован,
        НетСвязи
    }
}