using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace серьёзный.Core.CoreComputers
{
    public static class КартаКомпьютеров
    {
        private static readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "SeriousClub.db");

        private static readonly object блокировка = new();

        private static bool инициализировано;

        private static SqliteConnection Открыть()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(путь)!);

            var db = new SqliteConnection($"Data Source={путь}");

            db.Open();

            using (var pragma = db.CreateCommand())
            {
                // busy_timeout НЕ сохраняется в файле — его нужно
                // выставлять на КАЖДОМ новом соединении. Без этого
                // одновременное подключение нескольких ПК (например,
                // все 5 патрулей переподключаются сразу после
                // рестарта сервера) может упасть с SQLITE_BUSY вместо
                // того чтобы подождать до 5 секунд и записаться.
                pragma.CommandText =
                    "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";

                pragma.ExecuteNonQuery();
            }

            lock (блокировка)
            {
                if (!инициализировано)
                {
                    Инициализировать(db);
                    инициализировано = true;
                }
            }

            return db;
        }

        private static void Инициализировать(SqliteConnection db)
        {
            using var cmd = db.CreateCommand();

            cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS Computers(
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL DEFAULT '',
                WindowsName TEXT NOT NULL DEFAULT '',
                IP TEXT NOT NULL DEFAULT '',
                MAC TEXT NOT NULL DEFAULT ''
            );
            """;

            cmd.ExecuteNonQuery();

            cmd.CommandText = "SELECT COUNT(*) FROM Computers;";

            var count = Convert.ToInt32(cmd.ExecuteScalar());

            if (count > 0)
                return;

            var seed = new (int Id, string Name, string Win, string Ip, string Mac)[]
            {
                (1, "PC-01", "DESKTOP-IN5G5T1", "192.168.31.197", "34:5A:60:F4:E5:29"),
                (2, "PC-02", "DESKTOP-E079RMC", "192.168.31.55", "FC:9D:05:66:31:35"),
                (3, "PC-03", "DESKTOP-BOAJUJV", "192.168.31.150", "34:5A:60:F4:E5:F4"),
                (4, "PC-04", "DESKTOP-5S1UI1G", "192.168.31.204", "34:5A:60:F4:E5:30"),
                (5, "PC-05", "DESKTOP-TB208IO", "192.168.31.147", "34:5A:60:F4:E5:F1"),
                (100, "TEST-01", "DESKTOP-5P441FK", "192.168.0.237", "10-FF-E0-4C-98-9C")
            };

            foreach (var s in seed)
            {
                using var insert = db.CreateCommand();

                insert.CommandText =
                    "INSERT INTO Computers(Id, Name, WindowsName, IP, MAC) " +
                    "VALUES($id,$name,$win,$ip,$mac);";

                insert.Parameters.AddWithValue("$id", s.Id);
                insert.Parameters.AddWithValue("$name", s.Name);
                insert.Parameters.AddWithValue("$win", s.Win);
                insert.Parameters.AddWithValue("$ip", s.Ip);
                insert.Parameters.AddWithValue("$mac", s.Mac);

                insert.ExecuteNonQuery();
            }
        }

        public static IReadOnlyList<ЗаписьПК> Все
        {
            get
            {
                using var db = Открыть();

                var cmd = db.CreateCommand();

                cmd.CommandText =
                    "SELECT Id, Name, WindowsName, IP, MAC FROM Computers ORDER BY Id;";

                using var r = cmd.ExecuteReader();

                var список = new List<ЗаписьПК>();

                while (r.Read())
                {
                    список.Add(
                        new ЗаписьПК(
                            r.GetInt32(0),
                            r.GetString(1),
                            r.GetString(2),
                            r.GetString(3),
                            r.GetString(4)));
                }

                return список;
            }
        }

        public static ЗаписьПК? НайтиПоId(int id)
        {
            return Все.FirstOrDefault(x => x.Id == id);
        }

        public static ЗаписьПК? НайтиПоMAC(string mac)
        {
            var норм = Нормализовать(mac);

            return Все.FirstOrDefault(x => Нормализовать(x.MAC) == норм);
        }

        public static ЗаписьПК? НайтиПоИмениWindows(string имяWindows)
        {
            return Все.FirstOrDefault(
                x => string.Equals(x.ИмяWindows, имяWindows, StringComparison.OrdinalIgnoreCase));
        }

        // =====================================================
        // ДОБАВИТЬ / ИЗМЕНИТЬ / УДАЛИТЬ (используется админкой)
        // =====================================================

        public static void Добавить(int id, string название, string mac)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id должен быть больше 0.");

            if (string.IsNullOrWhiteSpace(название))
                throw new ArgumentException("Название не может быть пустым.");

            if (НайтиПоId(id) != null)
                throw new InvalidOperationException($"ПК с Id={id} уже существует.");

            using var db = Открыть();

            var cmd = db.CreateCommand();

            cmd.CommandText =
                "INSERT INTO Computers(Id, Name, WindowsName, IP, MAC) " +
                "VALUES($id,$name,'','', $mac);";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$name", название.Trim());
            cmd.Parameters.AddWithValue("$mac", mac?.Trim() ?? "");

            cmd.ExecuteNonQuery();
        }

        public static void Изменить(int id, string название, string mac)
        {
            if (string.IsNullOrWhiteSpace(название))
                throw new ArgumentException("Название не может быть пустым.");

            if (НайтиПоId(id) == null)
                throw new InvalidOperationException($"ПК с Id={id} не найден.");

            using var db = Открыть();

            var cmd = db.CreateCommand();

            cmd.CommandText =
                "UPDATE Computers SET Name=$name, MAC=$mac WHERE Id=$id;";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$name", название.Trim());
            cmd.Parameters.AddWithValue("$mac", mac?.Trim() ?? "");

            cmd.ExecuteNonQuery();
        }

        public static void Удалить(int id)
        {
            using var db = Открыть();

            var cmd = db.CreateCommand();

            cmd.CommandText = "DELETE FROM Computers WHERE Id=$id;";

            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }

        // =====================================================
        // АВТОРЕГИСТРАЦИЯ ПРИ HANDSHAKE
        // =====================================================

        public static void ЗарегистрироватьИлиОбновить(
            int id,
            string имяКомпьютера,
            string имяWindows,
            string ip,
            string mac)
        {
            var существующий = НайтиПоId(id);

            if (существующий == null)
            {
                using var db = Открыть();

                var cmd = db.CreateCommand();

                cmd.CommandText =
                    "INSERT INTO Computers(Id, Name, WindowsName, IP, MAC) " +
                    "VALUES($id,$name,$win,$ip,$mac);";

                cmd.Parameters.AddWithValue("$id", id);

                cmd.Parameters.AddWithValue(
                    "$name",
                    string.IsNullOrWhiteSpace(имяКомпьютера) ? $"ПК-{id}" : имяКомпьютера.Trim());

                cmd.Parameters.AddWithValue("$win", имяWindows ?? "");
                cmd.Parameters.AddWithValue("$ip", ip ?? "");
                cmd.Parameters.AddWithValue("$mac", mac ?? "");

                cmd.ExecuteNonQuery();

                return;
            }

            using var upd = Открыть();

            var updCmd = upd.CreateCommand();

            // Название, заданное админом вручную, не трогаем.
            // MAC дозаполняем только если он ещё не был задан.
            var новыйMac =
                string.IsNullOrWhiteSpace(существующий.MAC) && !string.IsNullOrWhiteSpace(mac)
                    ? mac
                    : существующий.MAC;

            updCmd.CommandText =
                "UPDATE Computers SET WindowsName=$win, IP=$ip, MAC=$mac WHERE Id=$id;";

            updCmd.Parameters.AddWithValue("$id", id);

            updCmd.Parameters.AddWithValue(
                "$win",
                string.IsNullOrWhiteSpace(имяWindows)
                    ? существующий.ИмяWindows
                    : имяWindows);

            updCmd.Parameters.AddWithValue(
                "$ip",
                string.IsNullOrWhiteSpace(ip)
                    ? существующий.IP
                    : ip);

            updCmd.Parameters.AddWithValue("$mac", новыйMac);

            updCmd.ExecuteNonQuery();
        }

        private static string Нормализовать(string? значение)
        {
            if (string.IsNullOrWhiteSpace(значение))
                return string.Empty;

            return значение
                .Replace(":", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim()
                .ToUpperInvariant();
        }
    }

    public class ЗаписьПК
    {
        public int Id { get; }

        public string Название { get; }

        public string ИмяWindows { get; }

        public string IP { get; }

        public string MAC { get; }

        public ЗаписьПК(
            int id,
            string название,
            string имяWindows,
            string ip,
            string mac)
        {
            Id = id;
            Название = название;
            ИмяWindows = имяWindows;
            IP = ip;
            MAC = mac;
        }
    }
}