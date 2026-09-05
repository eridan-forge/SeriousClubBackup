using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace серьёзный.патруль.Сервисы
{
    public static class СервисЭкрана
    {
        private static readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "SeriousClub.db");

        private static SqliteConnection Открыть()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(путь)!);

            var db =
                new SqliteConnection(
                    $"Data Source={путь}");

            db.Open();

            using var pragma =
                db.CreateCommand();

            pragma.CommandText =
                @"
PRAGMA journal_mode=WAL;
PRAGMA busy_timeout=3000;
";

            pragma.ExecuteNonQuery();

            using (var create = db.CreateCommand())
            {
                // Идемпотентно — на случай, если Патруль стартует раньше,
                // чем Экран Клуба на этом ПК хотя бы раз создал базу.
                create.CommandText =
                       @"
CREATE TABLE IF NOT EXISTS ScreenConfig
(
  Id INTEGER PRIMARY KEY CHECK(Id=1),
AdminName TEXT NOT NULL,
 Password TEXT NOT NULL,
Title TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ScreenState
(
 Id INTEGER PRIMARY KEY CHECK(Id=1),
Locked INTEGER NOT NULL,
 PcId INTEGER NOT NULL
);

INSERT OR IGNORE INTO ScreenConfig
VALUES(1, 'Администратор', '123456', 'Обратитесь к администратору');

INSERT OR IGNORE INTO ScreenState
VALUES(1, 1, 1);
"";
  create.ExecuteNonQuery();
            }
                ДобавитьКолонкуАккаунтаЕслиНужно(db);

            return db;
        }

        private static void ДобавитьКолонкуАккаунтаЕслиНужно(
            SqliteConnection db)
        {
            using var check = db.CreateCommand();

            check.CommandText = "PRAGMA table_info(ScreenState);";

            using (var reader = check.ExecuteReader())
            {
                while (reader.Read())
                {
                    var имя = reader.GetString(1);

                    if (имя == "AccountId")
                        return;
                }
            }

            try
            {
                using var alter = db.CreateCommand();

                alter.CommandText =
                    "ALTER TABLE ScreenState ADD COLUMN AccountId TEXT;";

                alter.ExecuteNonQuery();
            }
            catch
            {
                // Таблица ScreenState ещё не создана Экраном Клуба
                // на этом ПК — добавлять колонку некуда, пропускаем.
            }
        }

        // =========================================================
        // БЛОКИРОВКА ЭКРАНА КЛУБА
        // =========================================================

        public static void Заблокировать()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenState
SET Locked = 1
WHERE Id = 1;
";

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // РАЗБЛОКИРОВКА ЭКРАНА КЛУБА
        // =========================================================

        public static void Разблокировать()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenState
SET Locked = 0
WHERE Id = 1;
";

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // АККАУНТ, ПРИВЯЗАННЫЙ К ТЕКУЩЕМУ СЕАНСУ
        // =========================================================

        public static void УстановитьАккаунт(
            Guid? аккаунтId)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenState
SET AccountId = @acc
WHERE Id = 1;
";

            cmd.Parameters.AddWithValue(
                "@acc",
                (object?)аккаунтId?.ToString() ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // ПОЛУЧИТЬ СОСТОЯНИЕ
        // =========================================================

        public static bool Заблокирован()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
SELECT Locked
FROM ScreenState
WHERE Id = 1;
";

            var значение =
                cmd.ExecuteScalar();

            if (значение == null ||
                значение == DBNull.Value)
            {
                return true;
            }

            return Convert.ToInt32(значение) == 1;
        }

        // =========================================================
        // ПОЛУЧИТЬ ID ПК
        // =========================================================

        public static int ПолучитьIdПК()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
SELECT PcId
FROM ScreenState
WHERE Id = 1;
";

            var значение =
                cmd.ExecuteScalar();

            if (значение == null ||
                значение == DBNull.Value)
            {
                return 1;
            }

            return Convert.ToInt32(значение);
        }

        // =========================================================
        // УСТАНОВИТЬ ID ПК
        // =========================================================

        public static void УстановитьIdПК(
            int компьютерId)
        {
            if (компьютерId <= 0)
            {
                компьютерId = 1;
            }

            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenState
SET PcId = @id
WHERE Id = 1;
";

            cmd.Parameters.AddWithValue(
                "@id",
                компьютерId);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // СМЕНА ПАРОЛЯ ОБСЛУЖИВАНИЯ
        // =========================================================

        public static void СменитьПароль(
            string пароль)
        {
            пароль ??= string.Empty;

            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenConfig
SET Password = @p
WHERE Id = 1;
";

            cmd.Parameters.AddWithValue(
                "@p",
                пароль);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // СМЕНА ТЕКСТА ЭКРАНА
        // =========================================================

        public static void ИзменитьТекст(
            string текст)
        {
            текст ??= string.Empty;

            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenConfig
SET Title = @t
WHERE Id = 1;
";

            cmd.Parameters.AddWithValue(
                "@t",
                текст);

            cmd.ExecuteNonQuery();
        }

        // =========================================================
        // ПОЛУЧИТЬ ТЕКСТ ЭКРАНА
        // =========================================================

        public static string ПолучитьТекст()
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
SELECT Title
FROM ScreenConfig
WHERE Id = 1;
";

            var значение =
                cmd.ExecuteScalar();

            if (значение == null ||
                значение == DBNull.Value)
            {
                return "Обратитесь к администратору";
            }

            return Convert.ToString(значение)
                   ?? "Обратитесь к администратору";
        }
    }
}