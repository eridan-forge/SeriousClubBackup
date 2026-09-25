using System;

namespace серьёзный.Модели
{
    public class Отчёт
    {
        public DateTime НачалоПериода { get; set; }

        public DateTime КонецПериода { get; set; }

        public int КоличествоСеансов { get; set; }

        public TimeSpan ОбщееИгровоеВремя { get; set; }

        public decimal Выручка { get; set; }

        public decimal Возвраты { get; set; }

        public decimal Корректировки { get; set; }

        public decimal ИтоговаяСумма
        {
            get
            {
                return Выручка - Возвраты + Корректировки;
            }
        }
    }
}