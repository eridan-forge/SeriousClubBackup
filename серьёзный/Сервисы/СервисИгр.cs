using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class СервисИгр
    {
        private readonly string база;

        private readonly FileSystemWatcher watcher;

        public event Action<int>? ИгрыИзменились;

        public СервисИгр()
        {
            база =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub");

            Directory.CreateDirectory(
                Path.Combine(база, "Games"));

            Directory.CreateDirectory(
                Path.Combine(база, "Covers"));

            watcher =
                new FileSystemWatcher(
                    Path.Combine(база, "Games"),
                    "*.json");

            watcher.NotifyFilter =
                NotifyFilters.LastWrite |
                NotifyFilters.FileName;

            watcher.Changed += Обновлено;
            watcher.Created += Обновлено;
            watcher.Renamed += Переименовано;

            watcher.EnableRaisingEvents = true;
        }

        private void Обновлено(
            object sender,
            FileSystemEventArgs e)
        {
            var id = ПолучитьId(e.Name);

            if (id.HasValue)
                ИгрыИзменились?.Invoke(id.Value);
        }

        private void Переименовано(
            object sender,
            RenamedEventArgs e)
        {
            var id = ПолучитьId(e.Name);

            if (id.HasValue)
                ИгрыИзменились?.Invoke(id.Value);
        }

        private static int? ПолучитьId(
            string? имя)
        {
            if (string.IsNullOrWhiteSpace(имя))
                return null;

            имя =
                Path.GetFileNameWithoutExtension(имя);

            if (!имя.StartsWith("PC"))
                return null;

            if (int.TryParse(
                    имя.Substring(2),
                    out var id))
                return id;

            return null;
        }

        private string JsonПК(int id)
        {
            return Path.Combine(
                база,
                "Games",
                $"PC{id:000}.json");
        }

        private string CoversПК(int id)
        {
            var путь =
                Path.Combine(
                    база,
                    "Covers",
                    $"PC{id:000}");

            Directory.CreateDirectory(путь);

            return путь;
        }

        public List<Игра> ПолучитьИгры(int id)
        {
            var путь =
                JsonПК(id);

            if (!File.Exists(путь))
                return new();

            try
            {
                return JsonSerializer.Deserialize<List<Игра>>(
                           File.ReadAllText(путь))
                       ?? new();
            }
            catch
            {
                return new();
            }
        }

        public void СохранитьИгры(
            int id,
            List<Игра> игры)
        {
            игры =
                игры.OrderBy(x => x.Порядок)
                    .ToList();

            File.WriteAllText(
                JsonПК(id),
                JsonSerializer.Serialize(
                    игры,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }

        public string СкопироватьОбложку(
            int id,
            string исходныйФайл)
        {
            var папка =
                CoversПК(id);

            var новоеИмя =
                Guid.NewGuid() +
                Path.GetExtension(исходныйФайл);

            var новый =
                Path.Combine(
                    папка,
                    новоеИмя);

            File.Copy(
                исходныйФайл,
                новый,
                true);

            return новый;
        }

        // Новый путь для сохранения уже отрендеренного (обрезанного
        // через ОкноРедактораОбложки) изображения — без исходного
        // файла для копирования, поэтому результат всегда PNG.
        public string НовыйПутьОбложки(int id)
        {
            var папка =
                CoversПК(id);

            return Path.Combine(
                папка,
                Guid.NewGuid() + ".png");
        }

        public void Добавить(
            int id,
            Игра игра)
        {
            var игры =
                ПолучитьИгры(id);

            игра.Порядок =
                игры.Count;

            игры.Add(игра);

            СохранитьИгры(id, игры);
        }

        public void Изменить(
            int id,
            Игра игра)
        {
            var игры =
                ПолучитьИгры(id);

            var индекс =
                игры.FindIndex(x => x.Id == игра.Id);

            if (индекс < 0)
                return;

            игры[индекс] = игра;

            СохранитьИгры(id, игры);
        }

        public void Удалить(
            int id,
            Guid игра)
        {
            var игры =
                ПолучитьИгры(id);

            игры.RemoveAll(x => x.Id == игра);

            for (int i = 0; i < игры.Count; i++)
                игры[i].Порядок = i;

            СохранитьИгры(id, игры);
        }
    }
}