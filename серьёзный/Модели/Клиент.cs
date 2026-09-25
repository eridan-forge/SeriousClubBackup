using System;

namespace серьёзный.Модели
{
    public class Клиент
    {
        public int Id { get; set; }

        public string Имя { get; set; } = string.Empty;

        public DateTime ДатаСоздания { get; set; }

        public bool Активен { get; set; }

        public Клиент()
        {
            ДатаСоздания = DateTime.Now;
            Активен = true;
        }
    }
}