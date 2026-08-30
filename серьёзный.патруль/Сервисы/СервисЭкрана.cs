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

            return db;
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