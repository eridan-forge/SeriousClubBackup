using System;

namespace серьёзный.Модели
{
    public class ФинансоваяОперация
    {
        public long Id { get; set; }

        public DateTime Дата { get; set; }

        public ТипФинансовойОперации Тип { get; set; }

        public decimal Сумма { get; set; }

        public string Причина { get; set; } = string.Empty;

        public int? СеансId { get; set; }

        public string Комментарий { get; set; } = string.Empty;
    }

    public enum ТипФинансовойОперации
    {
        Оплата,
        Возврат,
        КорректировкаПлюс,
        КорректировкаМинус
    }
}