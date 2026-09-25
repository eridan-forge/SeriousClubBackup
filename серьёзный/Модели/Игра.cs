using System;

namespace серьёзный.Модели
{
    public class Игра
    {
        public Guid Id { get; set; } =
            Guid.NewGuid();

        public string Название { get; set; } =
            string.Empty;

        public string Категория { get; set; } =
            "Игры";

        public string Описание { get; set; } =
            string.Empty;

        public string Путь { get; set; } =
            string.Empty;

        public string Обложка { get; set; } =
            string.Empty;

        public int Порядок { get; set; }

        public bool Скрыта { get; set; }

        public string AppId { get; set; } = string.Empty;

        public string Launcher { get; set; } = string.Empty;
    }
}