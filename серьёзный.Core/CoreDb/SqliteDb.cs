using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace серьёзный.Core.CoreDb;

public static class SqliteDb
{
    public static readonly string Path =
        System.IO.Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    public static SqliteConnection Open()
    {
        Directory.CreateDirectory(
            System.IO.Path.GetDirectoryName(Path)!);

        var con = new SqliteConnection($"Data Source={Path}");

        con.Open();

        using var pragma = con.CreateCommand();

        pragma.CommandText =
            "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";

        pragma.ExecuteNonQuery();

        return con;
    }
}