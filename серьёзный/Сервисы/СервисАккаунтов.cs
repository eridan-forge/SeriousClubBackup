using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class СервисАккаунтов
    {
        private readonly string путь;

        private readonly СервисБазы001 база =
            new();

        private readonly List<АккаунтИгрока> аккаунты =
            new();

        private readonly object синхронизация =
            new();


        // =========================================================
        // КОНСТРУКТОР
        // =========================================================

        public СервисАккаунтов()
        {
            путь =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "SeriousClub",
                    "accounts.json");

            ЗагрузитьИзХранилища();
        }


        // =========================================================
        // ВСЕ АККАУНТЫ
        // =========================================================

        public IReadOnlyList<АккаунтИгрока> Все
        {
            get
            {
                lock (синхронизация)
                {
                    return аккаунты.ToList();
                }
            }
        }


        // =========================================================
        // ЗАГРУЗКА
        // =========================================================

        private void ЗагрузитьИзХранилища()
        {
            lock (синхронизация)
            {
                аккаунты.Clear();

                bool загруженоИзSQLite =
                    ЗагрузитьSQLiteВнутри();

                if (загруженоИзSQLite)
                {
                    НормализоватьВсеВнутри();
                    return;
                }

                ЗагрузитьJsonВнутри();

                if (аккаунты.Count == 0)
                {
                    return;
                }

                НормализоватьВсеВнутри();

                foreach (var аккаунт in аккаунты)
                {
                    СохранитьSQLiteВнутри(
                        аккаунт);
                }

                СохранитьJsonВнутри();
            }
        }


        // =========================================================
        // SQLITE
        // =========================================================

        private bool ЗагрузитьSQLiteВнутри()
        {
            using var db =
                база.Открыть();

            using var cmd =
                db.CreateCommand();

            /*
             * В новой схеме используем:
             *
             * Id
             * FirstName
             * Password
             * RemainingSeconds
             * PlayedSeconds
             * SessionCount
             * LastSession
             *
             * Старый LastName намеренно больше не читаем.
             */

            cmd.CommandText =
                @"
SELECT
    Id,
    FirstName,
    Password,
    RemainingSeconds,
    PlayedSeconds,
    SessionCount,
    LastSession
FROM Accounts
ORDER BY FirstName;";

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                try
                {
                    var аккаунт =
                        new АккаунтИгрока
                        {
                            Id =
                                Guid.Parse(
                                    reader.GetString(0)),

                            Имя =
                                reader.IsDBNull(1)
                                    ? string.Empty
                                    : reader.GetString(1),

                            Пароль =
                                reader.IsDBNull(2)
                                    ? string.Empty
                                    : reader.GetString(2),

                            ОсталосьВремени =
                                TimeSpan.FromSeconds(
                                    Math.Max(
                                        0,
                                        reader.GetInt64(3))),

                            ВсегоСыграно =
                                TimeSpan.FromSeconds(
                                    Math.Max(
                                        0,
                                        reader.GetInt64(4))),

                            ВсегоСеансов =
                                Math.Max(
                                    0,
                                    reader.GetInt32(5)),

                            ПоследнийСеанс =
                                reader.IsDBNull(6)
                                    ? null
                                    : БезопасноРазобратьДату(
                                        reader.GetString(6))
                        };

                    НормализоватьАккаунтВнутри(
                        аккаунт);

                    аккаунты.Add(
                        аккаунт);
                }
                catch
                {
                    // Повреждённая запись не должна
                    // ломать загрузку остальных аккаунтов.
                }
            }

            return аккаунты.Count > 0;
        }


        // =========================================================
        // JSON
        // =========================================================

        private void ЗагрузитьJsonВнутри()
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(путь)!);

                if (!File.Exists(путь))
                {
                    return;
                }

                var json =
                    File.ReadAllText(
                        путь);

                /*
                 * Сначала пробуем новую структуру.
                 */
                var список =
                    JsonSerializer.Deserialize<
                        List<АккаунтИгрока>>(
                        json);

                if (список != null)
                {
                    foreach (var аккаунт in список)
                    {
                        if (аккаунт == null)
                        {
                            continue;
                        }

                        НормализоватьАккаунтВнутри(
                            аккаунт);

                        аккаунты.Add(
                            аккаунт);
                    }

                    return;
                }
            }
            catch
            {
                // Ниже будет попытка миграции
                // старого JSON.
            }

            /*
             * Старый JSON мог содержать:
             *
             * Имя
             * Фамилия
             *
             * При миграции Фамилия рассматривается
             * как старый пароль.
             */

            try
            {
                using var документ =
                    JsonDocument.Parse(
                        File.ReadAllText(
                            путь));

                if (документ.RootElement.ValueKind !=
                    JsonValueKind.Array)
                {
                    return;
                }

                foreach (
                    var элемент
                    in документ.RootElement.EnumerateArray())
                {
                    try
                    {
                        var аккаунт =
                            СоздатьИзСтарогоJson(
                                элемент);

                        if (аккаунт == null)
                        {
                            continue;
                        }

                        НормализоватьАккаунтВнутри(
                            аккаунт);

                        аккаунты.Add(
                            аккаунт);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }


        // =========================================================
        // МИГРАЦИЯ СТАРОГО JSON
        // =========================================================

        private static АккаунтИгрока? СоздатьИзСтарогоJson(
            JsonElement элемент)
        {
            string имя =
                ПолучитьСтроку(
                    элемент,
                    "Имя");

            string староеВтороеПоле =
                ПолучитьСтроку(
                    элемент,
                    "Фамилия");

            string пароль =
                ПолучитьСтроку(
                    элемент,
                    "Пароль");

            if (string.IsNullOrWhiteSpace(пароль))
            {
                пароль =
                    староеВтороеПоле;
            }

            Guid id =
                ПолучитьGuid(
                    элемент,
                    "Id") ??
                Guid.NewGuid();

            return new АккаунтИгрока
            {
                Id = id,

                Имя = имя,

                Пароль = пароль,

                ОсталосьВремени =
                    ПолучитьTimeSpan(
                        элемент,
                        "ОсталосьВремени"),

                ВсегоСыграно =
                    ПолучитьTimeSpan(
                        элемент,
                        "ВсегоСыграно"),

                ВсегоСеансов =
                    ПолучитьInt(
                        элемент,
                        "ВсегоСеансов"),

                ПоследнийСеанс =
                    ПолучитьDateTime(
                        элемент,
                        "ПоследнийСеанс")
            };
        }


        // =========================================================
        // НОРМАЛИЗАЦИЯ
        // =========================================================

        private void НормализоватьВсеВнутри()
        {
            foreach (var аккаунт in аккаунты)
            {
                НормализоватьАккаунтВнутри(
                    аккаунт);
            }

            /*
             * Если исторически возникли дубликаты имён,
             * сохраняем только первую запись.
             *
             * Новые аккаунты с одинаковым именем не создаются.
             */
            var уникальные =
                аккаунты
                    .GroupBy(
                        x => x.Имя,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(
                        x => x.First())
                    .ToList();

            аккаунты.Clear();

            аккаунты.AddRange(
                уникальные);
        }


        private static void НормализоватьАккаунтВнутри(
            АккаунтИгрока аккаунт)
        {
            аккаунт.Имя =
                (аккаунт.Имя ?? string.Empty)
                    .Trim();

            аккаунт.Пароль =
                (аккаунт.Пароль ?? string.Empty)
                    .Trim();

            if (аккаунт.ОсталосьВремени <
                TimeSpan.Zero)
            {
                аккаунт.ОсталосьВремени =
                    TimeSpan.Zero;
            }

            if (аккаунт.ВсегоСыграно <
                TimeSpan.Zero)
            {
                аккаунт.ВсегоСыграно =
                    TimeSpan.Zero;
            }

            if (аккаунт.ВсегоСеансов < 0)
            {
                аккаунт.ВсегоСеансов =
                    0;
            }

            if (аккаунт.Id == Guid.Empty)
            {
                аккаунт.Id =
                    Guid.NewGuid();
            }
        }


        // =========================================================
        // СОХРАНЕНИЕ JSON
        // =========================================================

        private void СохранитьJsonВнутри()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(путь)!);

            File.WriteAllText(
                путь,
                JsonSerializer.Serialize(
                    аккаунты,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }


        // =========================================================
        // СОХРАНЕНИЕ SQLITE
        // =========================================================

        private void СохранитьSQLiteВнутри(
            АккаунтИгрока аккаунт)
        {
            using var db =
                база.Открыть();

            using var cmd =
                db.CreateCommand();

            /*
             * Новый код больше не пишет LastName.
             */
            cmd.CommandText =
                @"
INSERT OR REPLACE INTO Accounts
(
    Id,
    FirstName,
    Password,
    RemainingSeconds,
    PlayedSeconds,
    SessionCount,
    LastSession
)
VALUES
(
    @Id,
    @FirstName,
    @Password,
    @Remaining,
    @Played,
    @Sessions,
    @Last
);";

            cmd.Parameters.AddWithValue(
                "@Id",
                аккаунт.Id.ToString());

            cmd.Parameters.AddWithValue(
                "@FirstName",
                аккаунт.Имя);

            cmd.Parameters.AddWithValue(
                "@Password",
                аккаунт.Пароль);

            cmd.Parameters.AddWithValue(
                "@Remaining",
                Math.Max(
                    0L,
                    (long)
                        аккаунт
                            .ОсталосьВремени
                            .TotalSeconds));

            cmd.Parameters.AddWithValue(
                "@Played",
                Math.Max(
                    0L,
                    (long)
                        аккаунт
                            .ВсегоСыграно
                            .TotalSeconds));

            cmd.Parameters.AddWithValue(
                "@Sessions",
                Math.Max(
                    0,
                    аккаунт.ВсегоСеансов));

            cmd.Parameters.AddWithValue(
                "@Last",
                (object?)
                    аккаунт
                        .ПоследнийСеанс?
                        .ToString("O")
                    ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }


        private void СохранитьАккаунтВнутри(
            АккаунтИгрока аккаунт)
        {
            НормализоватьАккаунтВнутри(
                аккаунт);

            СохранитьSQLiteВнутри(
                аккаунт);

            СохранитьJsonВнутри();
        }


        // =========================================================
        // ПОИСК ПО ИМЕНИ
        // =========================================================

        public АккаунтИгрока? Найти(
            string имя)
        {
            имя =
                (имя ?? string.Empty)
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    имя))
            {
                return null;
            }

            lock (синхронизация)
            {
                return аккаунты.FirstOrDefault(
                    x =>
                        x.Имя.Equals(
                            имя,
                            StringComparison.OrdinalIgnoreCase));
            }
        }


        // =========================================================
        // ПОЛУЧЕНИЕ ПО ID
        // =========================================================

        public АккаунтИгрока? Получить(
            Guid id)
        {
            lock (синхронизация)
            {
                return НайтиВнутри(id);
            }
        }


        // =========================================================
        // АВТОРИЗАЦИЯ ИГРОКА
        // =========================================================

        public АккаунтИгрока? Авторизовать(
            string имя,
            string пароль)
        {
            имя =
                (имя ?? string.Empty)
                    .Trim();

            пароль =
                (пароль ?? string.Empty)
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    имя) ||
                string.IsNullOrWhiteSpace(
                    пароль))
            {
                return null;
            }

            lock (синхронизация)
            {
                return аккаунты.FirstOrDefault(
                    x =>
                        x.Имя.Equals(
                            имя,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            x.Пароль,
                            пароль,
                            StringComparison.Ordinal));
            }
        }


        // =========================================================
        // СОЗДАНИЕ АККАУНТА
        // =========================================================

        /*
         * Основной вариант.
         *
         * Администратор вводит:
         *
         * Имя
         * Пароль
         *
         * После этого аккаунт сохраняется.
         */
        public bool Создать(
            string имя,
            string пароль,
            out string ошибка)
        {
            lock (синхронизация)
            {
                ошибка =
                    string.Empty;

                имя =
                    (имя ?? string.Empty)
                        .Trim();

                пароль =
                    (пароль ?? string.Empty)
                        .Trim();

                if (string.IsNullOrWhiteSpace(
                        имя))
                {
                    ошибка =
                        "Имя обязательно.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                        пароль))
                {
                    ошибка =
                        "Пароль обязателен.";

                    return false;
                }

                if (аккаунты.Any(
                        x =>
                            x.Имя.Equals(
                                имя,
                                StringComparison.OrdinalIgnoreCase)))
                {
                    ошибка =
                        "Аккаунт с таким именем уже существует.";

                    return false;
                }

                var аккаунт =
                    new АккаунтИгрока
                    {
                        Id =
                            Guid.NewGuid(),

                        Имя =
                            имя,

                        Пароль =
                            пароль,

                        ОсталосьВремени =
                            TimeSpan.Zero,

                        ВсегоСыграно =
                            TimeSpan.Zero,

                        ВсегоСеансов =
                            0,

                        ПоследнийСеанс =
                            null
                    };

                аккаунты.Add(
                    аккаунт);

                try
                {
                    СохранитьАккаунтВнутри(
                         аккаунт);
                }
                catch (Exception исключение)
                {
                    аккаунты.Remove(
                        аккаунт);

                    ошибка =
                        "Не удалось сохранить аккаунт в базе данных: " +
                          исключение.Message;

                    return false;
                }
                return true;
            }
        }


        // =========================================================
        // ИЗМЕНЕНИЕ ПАРОЛЯ
        // =========================================================

        public bool ИзменитьПароль(
            Guid id,
            string новыйПароль)
        {
            новыйПароль =
                (новыйПароль ?? string.Empty)
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                    новыйПароль))
            {
                return false;
            }

            lock (синхронизация)
            {
                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    return false;
                }

                аккаунт.Пароль =
                    новыйПароль;

                СохранитьАккаунтВнутри(
                    аккаунт);

                return true;
            }
        }


        // =========================================================
        // УДАЛЕНИЕ
        // =========================================================

        public void Удалить(
            Guid id)
        {
            lock (синхронизация)
            {
                аккаунты.RemoveAll(
                    x =>
                        x.Id == id);

                using var db =
                    база.Открыть();

                using var cmd =
                    db.CreateCommand();

                cmd.CommandText =
                    "DELETE FROM Accounts WHERE Id=@Id;";

                cmd.Parameters.AddWithValue(
                    "@Id",
                    id.ToString());

                cmd.ExecuteNonQuery();

                СохранитьJsonВнутри();
            }
        }


        // =========================================================
        // ДОБАВИТЬ ВРЕМЯ
        // =========================================================

        public void ДобавитьВремя(
            Guid id,
            TimeSpan время)
        {
            if (время <= TimeSpan.Zero)
            {
                return;
            }

            lock (синхронизация)
            {
                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    return;
                }

                try
                {
                    аккаунт.ОсталосьВремени +=
                        время;
                }
                catch (OverflowException)
                {
                    аккаунт.ОсталосьВремени =
                        TimeSpan.MaxValue;
                }

                СохранитьАккаунтВнутри(
                    аккаунт);
            }
        }


        // =========================================================
        // УСТАНОВИТЬ ОСТАТОК
        // =========================================================

        public void УстановитьОстаток(
            Guid id,
            TimeSpan остаток)
        {
            lock (синхронизация)
            {
                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    return;
                }

                аккаунт.ОсталосьВремени =
                    остаток < TimeSpan.Zero
                        ? TimeSpan.Zero
                        : остаток;

                СохранитьАккаунтВнутри(
                    аккаунт);
            }
        }


        // =========================================================
        // СПИСАТЬ ВРЕМЯ
        // =========================================================

        public void СписатьВремя(
            Guid id,
            TimeSpan время)
        {
            if (время <= TimeSpan.Zero)
            {
                return;
            }

            lock (синхронизация)
            {
                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    return;
                }

                аккаунт.ОсталосьВремени =
                    SafeSubtract(
                        аккаунт.ОсталосьВремени,
                        время);

                СохранитьАккаунтВнутри(
                    аккаунт);
            }
        }


        // =========================================================
        // ПОИСК ДЛЯ АДМИНИСТРАТОРА
        // =========================================================

        /*
         * Пароль здесь НЕ требуется.
         *
         * Администратор выбирает аккаунт из списка,
         * поэтому проверять пароль повторно не нужно.
         */
        public List<АккаунтИгрока> Искать(
            string имя)
        {
            имя ??=
                string.Empty;

            имя =
                имя.Trim();

            lock (синхронизация)
            {
                return аккаунты
                    .Where(
                        x =>
                            x.Имя.StartsWith(
                                имя,
                                StringComparison.OrdinalIgnoreCase))
                    .OrderBy(
                        x => x.Имя)
                    .ToList();
            }
        }


        // =========================================================
        // СТАТИСТИКА ВО ВРЕМЯ СЕАНСА
        // =========================================================

        /*
         * Здесь обновляется ТОЛЬКО статистика.
         *
         * Остаток времени здесь НЕ списывается.
         *
         * СервисСеансов является единственным местом,
         * которое расходует баланс текущего сеанса.
         */
        public void ОбновитьВоВремяСеанса(
            Guid id,
            TimeSpan прошло,
            bool списыватьБаланс)
        {
            if (прошло <= TimeSpan.Zero)
            {
                return;
            }

            lock (синхронизация)
            {
                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    return;
                }

                try
                {
                    аккаунт.ВсегоСыграно +=
                        прошло;
                }
                catch (OverflowException)
                {
                    аккаунт.ВсегоСыграно =
                        TimeSpan.MaxValue;
                }

                аккаунт.ПоследнийСеанс =
                    DateTime.Now;

                _ =
                    списыватьБаланс;

                СохранитьSQLiteВнутри(
                    аккаунт);
            }
        }


        // =========================================================
        // ЗАВЕРШЕНИЕ СТАТИСТИКИ
        // =========================================================

        public void ЗавершитьСтатистику(
            Guid id)
        {
            lock (синхронизация)
            {
                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    return;
                }

                if (аккаунт.ВсегоСеансов <
                    int.MaxValue)
                {
                    аккаунт.ВсегоСеансов++;
                }

                аккаунт.ПоследнийСеанс =
                    DateTime.Now;

                СохранитьАккаунтВнутри(
                    аккаунт);
            }
        }


        // =========================================================
        // ВСПОМОГАТЕЛЬНЫЕ
        // =========================================================

        private АккаунтИгрока? НайтиВнутри(
            Guid id)
        {
            return аккаунты.FirstOrDefault(
                x =>
                    x.Id == id);
        }


        private static DateTime? БезопасноРазобратьДату(
            string значение)
        {
            if (DateTime.TryParse(
                    значение,
                    out var дата))
            {
                return дата;
            }

            return null;
        }


        private static string ПолучитьСтроку(
            JsonElement элемент,
            string имя)
        {
            if (!элемент.TryGetProperty(
                    имя,
                    out var свойство))
            {
                return string.Empty;
            }

            return свойство.ValueKind ==
                   JsonValueKind.String
                ? свойство.GetString() ??
                    string.Empty
                : string.Empty;
        }


        private static Guid? ПолучитьGuid(
            JsonElement элемент,
            string имя)
        {
            var строка =
                ПолучитьСтроку(
                    элемент,
                    имя);

            if (Guid.TryParse(
                    строка,
                    out var guid))
            {
                return guid;
            }

            return null;
        }


        private static TimeSpan ПолучитьTimeSpan(
            JsonElement элемент,
            string имя)
        {
            if (!элемент.TryGetProperty(
                    имя,
                    out var свойство))
            {
                return TimeSpan.Zero;
            }

            if (свойство.ValueKind ==
                JsonValueKind.String)
            {
                var строка =
                    свойство.GetString();

                if (TimeSpan.TryParse(
                        строка,
                        out var время))
                {
                    return
                        время < TimeSpan.Zero
                            ? TimeSpan.Zero
                            : время;
                }
            }

            if (свойство.ValueKind ==
                JsonValueKind.Number &&
                свойство.TryGetInt64(
                    out var секунды))
            {
                return TimeSpan.FromSeconds(
                    Math.Max(
                        0,
                        секунды));
            }

            return TimeSpan.Zero;
        }


        private static int ПолучитьInt(
            JsonElement элемент,
            string имя)
        {
            if (!элемент.TryGetProperty(
                    имя,
                    out var свойство))
            {
                return 0;
            }

            if (свойство.ValueKind ==
                    JsonValueKind.Number &&
                свойство.TryGetInt32(
                    out var значение))
            {
                return Math.Max(
                    0,
                    значение);
            }

            return 0;
        }


        private static DateTime? ПолучитьDateTime(
            JsonElement элемент,
            string имя)
        {
            var строка =
                ПолучитьСтроку(
                    элемент,
                    имя);

            if (DateTime.TryParse(
                    строка,
                    out var дата))
            {
                return дата;
            }

            return null;
        }


        private static TimeSpan SafeSubtract(
            TimeSpan значение,
            TimeSpan вычесть)
        {
            if (вычесть <= TimeSpan.Zero)
            {
                return значение;
            }

            if (значение <= вычесть)
            {
                return TimeSpan.Zero;
            }

            return значение - вычесть;
        }

        public List<АккаунтИгрока> ПолучитьВсе()
        {
            lock (синхронизация)
            {
                return аккаунты
                    .OrderBy(x => x.Имя)
                    .ToList();
            }
        }




    }
}