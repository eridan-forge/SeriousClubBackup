using System;

namespace серьёзный.Модели
{
    public class ЗаписьЧата
    {
        public long Id { get; set; }

        public int КомпьютерId { get; set; }

        public string Имя { get; set; } = "";

        public string Текст { get; set; } = "";

        public DateTime Время { get; set; } =
            DateTime.Now;

        public bool ОтАдминистратора { get; set; }

        public bool Прочитано { get; set; }

        public Guid? АккаунтGuid { get; set; }

        public string ВремяСтрокой =>
            Время.ToString("HH:mm");
    }
}