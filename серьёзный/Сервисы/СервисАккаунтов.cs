using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using серьёзный.Модели;
using серьёзный.Core.CoreSecurity;

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

            cmd.CommandText =
                @"
SELECT
    Id,
    FirstName,
    Password,
    RemainingSeconds,
    PlayedSeconds,
    SessionCount,
    LastSession,
    Phone
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
                                        reader.GetString(6)),

                            Телефон =
                                reader.IsDBNull(7)
                                    ? string.Empty
                                    : reader.GetString(7)
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

                var список =
                    JsonSerializer.Deserialize<List<АккаунтИгрока>>(
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

                Телефон =
                    ПолучитьСтроку(
                        элемент,
                        "Телефон"),

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

            аккаунт.Телефон =
                НормализоватьТелефон(аккаунт.Телефон);

            if (аккаунт.ОсталосьВремени 
                TimeSpan.Zero)
            {
                аккаунт.ОсталосьВремени =
                    TimeSpan.Zero;
            }

            if (аккаунт.ВсегоСыграно 
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
    LastSession,
    Phone
)
VALUES
(
    @Id,
    @FirstName,
    @Password,
    @Remaining,
    @Played,
    @Sessions,
    @Last,
    @Phone
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

            cmd.Parameters.AddWithValue(
                "@Phone",
                аккаунт.Телефон ?? string.Empty);

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
        // ПОИСК ПО ИМЕНИ (точное совпадение, оставлено для совместимости)
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
        // ПОИСК ПО ТЕЛЕФОНУ
        // =========================================================

        public АккаунтИгрока? НайтиПоТелефону(
            string телефон)
        {
            var норм =
                НормализоватьТелефон(телефон);

            if (string.IsNullOrWhiteSpace(норм))
            {
                return null;
            }

            lock (синхронизация)
            {
                return аккаунты.FirstOrDefault(
                    x =>
                        НормализоватьТелефон(x.Телефон) ==
                        норм);
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

        public АккаунтИгрока? ПеречитатьИзБазы(Guid id)
        {
            using var db = база.Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText = @"
SELECT
    Id,
    FirstName,
    Password,
    RemainingSeconds,
    PlayedSeconds,
    SessionCount,
    LastSession,
    Phone
FROM Accounts
WHERE Id=@Id;";

            cmd.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            var свежий = new АккаунтИгрока
            {
                Id = Guid.Parse(reader.GetString(0)),
                Имя = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Пароль = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ОсталосьВремени = TimeSpan.FromSeconds(Math.Max(0, reader.GetInt64(3))),
                ВсегоСыграно = TimeSpan.FromSeconds(Math.Max(0, reader.GetInt64(4))),
                ВсегоСеансов = Math.Max(0, reader.GetInt32(5)),
                ПоследнийСеанс = reader.IsDBNull(6)
                    ? null
                    : БезопасноРазобратьДату(reader.GetString(6)),
                Телефон = reader.IsDBNull(7) ? "" : reader.GetString(7)
            };

            lock (синхронизация)
            {
                var индекс = аккаунты.FindIndex(x => x.Id == id);

                if (индекс >= 0)
                    аккаунты[индекс] = свежий;
                else
                    аккаунты.Add(свежий);
            }

            return свежий;
        }


        // =========================================================
        // АВТОРИЗАЦИЯ ПО ИМЕНИ (оставлено, сейчас не используется входом)
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
                var найденный =
                    аккаунты.FirstOrDefault(
                        x =>
                            x.Имя.Equals(
                                имя,
                                StringComparison.OrdinalIgnoreCase));

                if (найденный == null)
                {
                    return null;
                }

                if (PasswordHasher.IsHashed(найденный.Пароль))
                {
                    return PasswordHasher.Verify(
                        пароль,
                        найденный.Пароль)
                        ? найденный
                        : null;
                }

                if (!string.Equals(
                        найденный.Пароль,
                        пароль,
                        StringComparison.Ordinal))
                {
                    return null;
                }

                найденный.Пароль =
                    PasswordHasher.Hash(пароль);

                СохранитьАккаунтВнутри(
                    найденный);

                return найденный;
            }
        }


        // =========================================================
        // АВТОРИЗАЦИЯ ПО ТЕЛЕФОНУ — используется экраном входа
        // =========================================================

        public АккаунтИгрока? АвторизоватьПоТелефону(
            string телефон,
            string пароль)
        {
            var норм =
                НормализоватьТелефон(телефон);

            пароль =
                (пароль ?? string.Empty)
                    .Trim();

            if (string.IsNullOrWhiteSpace(норм) ||
                string.IsNullOrWhiteSpace(пароль))
            {
                return null;
            }

            lock (синхронизация)
            {
                var найденный =
                    аккаунты.FirstOrDefault(
                        x =>
                            НормализоватьТелефон(x.Телефон) ==
                            норм);

                if (найденный == null)
                {
                    return null;
                }

                if (PasswordHasher.IsHashed(найденный.Пароль))
                {
                    return PasswordHasher.Verify(
                        пароль,
                        найденный.Пароль)
                        ? найденный
                        : null;
                }

                if (!string.Equals(
                        найденный.Пароль,
                        пароль,
                        StringComparison.Ordinal))
                {
                    return null;
                }

                найденный.Пароль =
                    PasswordHasher.Hash(пароль);

                СохранитьАккаунтВнутри(
                    найденный);

                return найденный;
            }
        }


        // =========================================================
        // СОЗДАНИЕ АККАУНТА — телефон + пароль + ФИО
        // =========================================================

        public bool Создать(
            string телефон,
            string пароль,
            string полноеИмя,
            out string ошибка)
        {
            lock (синхронизация)
            {
                ошибка =
                    string.Empty;

                var нормТелефон =
                    НормализоватьТелефон(телефон);

                пароль =
                    (пароль ?? string.Empty)
                        .Trim();

                полноеИмя =
                    (полноеИмя ?? string.Empty)
                        .Trim();

                if (нормТелефон.Length != 11 ||
                    нормТелефон[0] != '7')
                {
                    ошибка =
                        "Введите корректный номер телефона (10 или 11 цифр).";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(пароль))
                {
                    ошибка =
                        "Пароль обязателен.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(полноеИмя))
                {
                    ошибка =
                        "Введите ФИО.";

                    return false;
                }

                if (аккаунты.Any(
                        x =>
                            НормализоватьТелефон(x.Телефон) ==
                            нормТелефон))
                {
                    ошибка =
                        "Аккаунт с таким номером телефона уже существует.";

                    return false;
                }

                var аккаунт =
                    new АккаунтИгрока
                    {
                        Id =
                            Guid.NewGuid(),

                        Имя =
                            полноеИмя,

                        Телефон =
                            нормТелефон,

                        Пароль =
                            PasswordHasher.Hash(пароль),

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
        // ИЗМЕНИТЬ/ПРИВЯЗАТЬ ТЕЛЕФОН СУЩЕСТВУЮЩЕМУ АККАУНТУ
        // (нужно для миграции старых аккаунтов без телефона)
        // =========================================================

        public bool УстановитьТелефон(
            Guid id,
            string телефон,
            out string ошибка)
        {
            ошибка = string.Empty;

            var нормТелефон =
                НормализоватьТелефон(телефон);

            if (нормТелефон.Length != 11 ||
                нормТелефон[0] != '7')
            {
                ошибка =
                    "Введите корректный номер телефона (10 или 11 цифр).";

                return false;
            }

            lock (синхронизация)
            {
                if (аккаунты.Any(
                        x =>
                            x.Id != id &&
                            НормализоватьТелефон(x.Телефон) ==
                            нормТелефон))
                {
                    ошибка =
                        "Этот номер телефона уже привязан к другому аккаунту.";

                    return false;
                }

                var аккаунт =
                    НайтиВнутри(id);

                if (аккаунт == null)
                {
                    ошибка =
                        "Аккаунт не найден.";

                    return false;
                }

                аккаунт.Телефон =
                    нормТелефон;

                СохранитьАккаунтВнутри(
                    аккаунт);

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
                    PasswordHasher.Hash(новыйПароль);

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
        // ПОИСК ДЛЯ АДМИНИСТРАТОРА — по имени ИЛИ по цифрам телефона
        // =========================================================

        public List<АккаунтИгрока> Искать(
            string текст)
        {
            текст ??=
                string.Empty;

            текст =
                текст.Trim();

            var цифрыПоиска =
                new string(
                    текст.Where(char.IsDigit).ToArray());

            lock (синхронизация)
            {
                return аккаунты
                    .Where(
                        x =>
                            x.Имя.StartsWith(
                                текст,
                                StringComparison.OrdinalIgnoreCase) ||
                            (цифрыПоиска.Length > 0 &&
                             !string.IsNullOrWhiteSpace(x.Телефон) &&
                             x.Телефон.Contains(цифрыПоиска)))
                    .OrderBy(
                        x => x.Имя)
                    .ToList();
            }
        }


        // =========================================================
        // СТАТИСТИКА ВО ВРЕМЯ СЕАНСА
        // =========================================================

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

                if (аккаунт.ВсегоСеансов 
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


        // Приводит телефон к виду "7ХХХХХХХХХХ" (11 цифр) независимо
        // от того, как он введён: +7, 8, без префикса, с пробелами,
        // скобками, дефисами и т.д. Используется и при сохранении
        // (канонический вид в базе), и при сравнении (поиск/вход).
        private static string НормализоватьТелефон(string? телефон)
        {
            if (string.IsNullOrWhiteSpace(телефон))
                return string.Empty;

            var цифры =
                new string(
                    телефон.Where(char.IsDigit).ToArray());

            if (цифры.Length == 0)
                return string.Empty;

            if (цифры[0] == '8')
                цифры = "7" + цифры.Substring(1);
            else if (цифры[0] != '7')
                цифры = "7" + цифры;

            if (цифры.Length > 11)
                цифры = цифры.Substring(0, 11);

            return цифры;
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