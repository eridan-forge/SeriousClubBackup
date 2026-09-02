using Microsoft.Data.Sqlite;
using System.IO;

namespace серьёзный.Core.CoreServices;

public class GameSessionReportRecord
{
    public long Id { get; set; }

    public Guid AccountId { get; set; }

    public int PcId { get; set; }

    public long PlayedSeconds { get; set; }
}

// Локальная очередь на клиентском ПК (ЭкранКлуба -> Патруль -> сервер).
// ЭкранКлуба сама НИЧЕГО не пишет в баланс/статистику аккаунта -
// она только кладёт сюда факт "аккаунт X доиграл Y секунд на ПК Z".
// Патруль на этом же физическом ПК забирает запись, шлёт её по сети
// на сервер, и только сервер применяет её к Accounts.
public static class GameSessionReportBridgeService
{
    private static readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private static SqliteConnection Open() => серьёзный.Core.CoreDb.SqliteDb.Open();

    // Вызывает ЭкранКлуба, когда игра на клиентском ПК закрылась.
    public static void CreateRequest(Guid accountId, int pcId, long playedSeconds)
    {
        if (playedSeconds <= 0)
            return;

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO GameSessionReports(AccountId, PcId, PlayedSeconds, Done, Created) " +
            "VALUES($a, $pc, $s, 0, $t);";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());
        cmd.Parameters.AddWithValue("$pc", pcId);
        cmd.Parameters.AddWithValue("$s", playedSeconds);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    // Вызывает Патруль: берёт следующий неотправленный отчёт.
    public static GameSessionReportRecord? TakeNextPending()
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, AccountId, PcId, PlayedSeconds FROM GameSessionReports " +
            "WHERE Done=0 ORDER BY Id LIMIT 1;";

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new GameSessionReportRecord
        {
            Id = r.GetInt64(0),
            AccountId = Guid.Parse(r.GetString(1)),
            PcId = r.GetInt32(2),
            PlayedSeconds = r.GetInt64(3)
        };
    }

    // Вызывает Патруль после подтверждения сервером. Если сервер не
    // подтвердил (сеть/таймаут) - Done остаётся 0, отчёт уйдёт снова.
    public static void MarkDone(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "UPDATE GameSessionReports SET Done=1 WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }

    public static void Cleanup(TimeSpan olderThan)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "DELETE FROM GameSessionReports WHERE Done=1 AND Created < $t;";
        cmd.Parameters.AddWithValue("$t", (DateTime.Now - olderThan).ToString("O"));

        cmd.ExecuteNonQuery();
    }
}