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
";
                create.ExecuteNonQuery();
            }

            ДобавитьКолонкуАккаунтаЕслиНужно(db);

            ДобавитьКолонкуТемыЕслиНужно(db);

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

        // Аналогично ДобавитьКолонкуАккаунтаЕслиНужно выше — колонка
        // ThemeId может отсутствовать в базе, созданной до этого фикса.
        private static void ДобавитьКолонкуТемыЕслиНужно(
            SqliteConnection db)
        {
            using var check = db.CreateCommand();

            check.CommandText = "PRAGMA table_info(ScreenConfig);";

            using (var reader = check.ExecuteReader())
            {
                while (reader.Read())
                {
                    var имя = reader.GetString(1);

                    if (имя == "ThemeId")
                        return;
                }
            }

            try
            {
                using var alter = db.CreateCommand();

                alter.CommandText =
                    "ALTER TABLE ScreenConfig ADD COLUMN ThemeId TEXT NOT NULL DEFAULT '';";

                alter.ExecuteNonQuery();
            }
            catch
            {
                // Таблица ещё не создана Экраном Клуба на этом ПК —
                // добавлять колонку некуда, пропускаем.
            }
        }

        // Вызывается по сети командой КомандаПК.УстановитьТемуВхода —
        // Патруль пишет присланный Id темы в локальную базу этого ПК,
        // а MainWindow Экрана Клуба (тот же ПК) подхватывает его сама
        // через свой таймер и меняет видео/лого без перезапуска.
        public static void УстановитьТему(
            string themeId)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();

            cmd.CommandText =
                @"
UPDATE ScreenConfig
SET ThemeId = @t
WHERE Id = 1;
";

            cmd.Parameters.AddWithValue(
                "@t",
                themeId ?? "");

            cmd.ExecuteNonQuery();
        }

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