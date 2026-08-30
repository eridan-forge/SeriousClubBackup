using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace серьёзный.Патруль.Система
{
    public static class КартаКомпьютеров
    {
        private static readonly string путь =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub",
                "computers.db");

        private static List<ЗаписьПК> кэш = new();

        static КартаКомпьютеров()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(путь)!);

            using var db = Открыть();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Computers (
                    Id INTEGER PRIMARY KEY,
                    Название TEXT NOT NULL,
                    MAC TEXT NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();

            Обновить();
        }

        private static SqliteConnection Открыть()
        {
            var db = new SqliteConnection($"Data Source={путь}");
            db.Open();
            return db;
        }

        public static IReadOnlyList<ЗаписьПК> Все => кэш;

        public static void Обновить()
        {
            var список = new List<ЗаписьПК>();

            using var db = Открыть();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT Id, Название, MAC FROM Computers ORDER BY Id;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                список.Add(new ЗаписьПК(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));

            кэш = список;
        }

        public static ЗаписьПК? НайтиПоId(int id) =>
            кэш.FirstOrDefault(x => x.Id == id);

        public static void Добавить(int id, string название, string mac)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "INSERT INTO Computers (Id, Название, MAC) VALUES (@id, @n, @m);";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@n", название);
            cmd.Parameters.AddWithValue("@m", mac ?? "");
            cmd.ExecuteNonQuery();

            Обновить();
        }

        public static void Изменить(int id, string название, string mac)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "UPDATE Computers SET Название=@n, MAC=@m WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@n", название);
            cmd.Parameters.AddWithValue("@m", mac ?? "");
            cmd.ExecuteNonQuery();

            Обновить();
        }

        public static void Удалить(int id)
        {
            using var db = Открыть();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM Computers WHERE Id=@id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            Обновить();
        }
    }

    public class ЗаписьПК
    {
        public int Id { get; }
        public string Название { get; }
        public string MAC { get; }

        public ЗаписьПК(int id, string название, string mac)
        {
            Id = id;
            Название = название;
            MAC = mac;
        }
    }
}