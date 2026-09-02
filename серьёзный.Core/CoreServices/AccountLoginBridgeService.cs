using Microsoft.Data.Sqlite;
using System.IO;

namespace серьёзный.Core.CoreServices;

public enum LoginRequestStatus
{
    Pending,
    Success,
    Failed
}

public class LoginRequestRecord
{
    public long Id { get; set; }

    public string Login { get; set; } = "";

    public string Password { get; set; } = "";

    public LoginRequestStatus Status { get; set; } = LoginRequestStatus.Pending;

    public Guid? AccountId { get; set; }

    public string? FullName { get; set; }

    public long RemainingSeconds { get; set; }

    public string? Error { get; set; }

    public DateTime Created { get; set; } = DateTime.Now;
}

// Очередь запросов на вход, лежащая в ЛОКАЛЬНОЙ базе того же ПК.
// ЭкранКлуба кладёт запрос сюда, Патруль (тот же ПК) забирает его,
// пересылает по сети на сервер, и пишет ответ обратно сюда же.
// Это не второй источник данных об аккаунтах — это просто очередь
// для межпроцессного общения ЭкранКлуба <-> Патруль на одной машине.
public static class AccountLoginBridgeService
{
    private static readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private static SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        var con = new SqliteConnection($"Data Source={db}");
        con.Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS AccountLoginRequests(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Login TEXT NOT NULL,
            Password TEXT NOT NULL,
            Status INTEGER NOT NULL DEFAULT 0,
            AccountId TEXT,
            FullName TEXT,
            RemainingSeconds INTEGER NOT NULL DEFAULT 0,
            Error TEXT,
            Created TEXT NOT NULL
        );
        """;

        cmd.ExecuteNonQuery();

        return con;
    }

    // Вызывает ЭкранКлуба при нажатии "Войти".
    public static long CreateRequest(string login, string password)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO AccountLoginRequests(Login, Password, Status, Created) " +
            "VALUES($l, $p, 0, $t);";

        cmd.Parameters.AddWithValue("$l", login);
        cmd.Parameters.AddWithValue("$p", password);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();

        idCmd.CommandText = "SELECT last_insert_rowid();";

        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    // Вызывает Патруль: берёт следующий необработанный запрос.
    public static LoginRequestRecord? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Login, Password FROM AccountLoginRequests " +
            "WHERE Status=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new LoginRequestRecord
        {
            Id = r.GetInt64(0),
            Login = r.GetString(1),
            Password = r.GetString(2)
        };
    }

    // Вызывает Патруль после ответа сервера.
    public static void CompleteRequest(
        long id,
        bool success,
        Guid? accountId,
        string? fullName,
        long remainingSeconds,
        string? error)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        UPDATE AccountLoginRequests
        SET Status=$s, AccountId=$acc, FullName=$name,
            RemainingSeconds=$rem, Error=$err
        WHERE Id=$id;
        """;

        cmd.Parameters.AddWithValue("$s", success ? 1 : 2);
        cmd.Parameters.AddWithValue("$acc", (object?)accountId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$name", (object?)fullName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rem", remainingSeconds);
        cmd.Parameters.AddWithValue("$err", (object?)error ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    // Вызывает ЭкранКлуба: опрашивает результат по Id.
    public static LoginRequestRecord? GetResult(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Status, AccountId, FullName, RemainingSeconds, Error " +
            "FROM AccountLoginRequests WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new LoginRequestRecord
        {
            Id = id,
            Status = (LoginRequestStatus)r.GetInt32(0),
            AccountId = r.IsDBNull(1) ? null : Guid.Parse(r.GetString(1)),
            FullName = r.IsDBNull(2) ? null : r.GetString(2),
            RemainingSeconds = r.GetInt64(3),
            Error = r.IsDBNull(4) ? null : r.GetString(4)
        };
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM AccountLoginRequests WHERE Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}