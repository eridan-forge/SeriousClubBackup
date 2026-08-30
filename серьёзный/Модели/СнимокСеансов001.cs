using System;
using System.Collections.Generic;

namespace серьёзный.Модели
{
    public class СнимокСеансов001
    {
        public DateTime ВремяСохранения { get; set; }

        public List<Сеанс001> Сеансы { get; set; } =
            new();
    }

    public class Сеанс001
    {
        public int Id { get; set; }

        public int КомпьютерId { get; set; }

        public string ИмяКлиента { get; set; } = "";

        public Guid? АккаунтGuid { get; set; }

        public TimeSpan КупленноеВремя { get; set; }

        public TimeSpan ВремяАккаунта { get; set; }

        public bool ИспользуетсяОстатокАккаунта { get; set; }

        public DateTime Начало { get; set; }

        public DateTime ЗапланированноеОкончание { get; set; }

        public decimal Стоимость { get; set; }
    }
}