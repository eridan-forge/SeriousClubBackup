using System;
using System.Collections.Generic;

namespace серьёзный.Модели
{
    public class АрхивАварий005
    {
        public List<ЗаписьАварии005> Записи { get; set; } =
            new();
    }

    public class ЗаписьАварии005
    {
        public int Id { get; set; }

        public int КомпьютерId { get; set; }

        public string Игрок { get; set; } = "";

        public Guid? АккаунтGuid { get; set; }

        public DateTime Начало { get; set; }

        public DateTime Отключение { get; set; }

        public TimeSpan Сыграно { get; set; }

        public TimeSpan Возвращено { get; set; }

        public decimal Стоимость { get; set; }

        public string Причина { get; set; } =
            "Аварийное отключение";
    }
}