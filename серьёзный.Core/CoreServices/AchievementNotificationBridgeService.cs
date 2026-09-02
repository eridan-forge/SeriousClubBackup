using Microsoft.Data.Sqlite;
using System.IO;

namespace серьёзный.Core.CoreServices;

public class AchievementNotificationRecord
{
    public long Id { get; set; }

    public Guid AccountId { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";
}

// Локальный мост НА ФИЗИЧЕСКОМ ПК ИГРОКА (та же роль, что у
// GameSessionReportBridgeService/AccountBalanceBridgeService):
// Патруль пишет сюда, когда по сети от сервера пришла команда
// ДостижениеРазблокировано, ОкноИгрока на этом же ПК читает
// и показывает тост.
public static class AchievementNotificationBridgeService
{
    private static readonly string db =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub",
            "SeriousClub.db");

    private static SqliteConnection Open() => серьёзный.Core.CoreDb.SqliteDb.Open();

    // Вызывает Патруль, когда от сервера пришла команда
    // ДостижениеРазблокировано.
    public static void Enqueue(Guid accountId, string name, string description)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "INSERT INTO AchievementNotifications(AccountId, Name, Description, Delivered, Created) " +
            "VALUES($a,$n,$d,0,$t);";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$d", description);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();

        // Отдельного фонового воркера для этой таблицы нет (в отличие
        // от GameSessionReportBridge) — чистим старое прямо тут, раз
        // строки добавляются нечасто.
        var cleanup = con.CreateCommand();

        cleanup.CommandText =
            "DELETE FROM AchievementNotifications WHERE Delivered=1 AND Created < $t;";

        cleanup.Parameters.AddWithValue(
            "$t",
            (DateTime.Now - TimeSpan.FromDays(7)).ToString("O"));

        cleanup.ExecuteNonQuery();
    }

    // Вызывает ОкноИгрока на этом же ПК — берёт следующее непоказанное
    // уведомление именно этого аккаунта.
    public static AchievementNotificationRecord? TakeNextPending(Guid accountId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Name, Description FROM AchievementNotifications " +
            "WHERE AccountId=$a AND Delivered=0 ORDER BY Id LIMIT 1;";

        cmd.Parameters.AddWithValue("$a", accountId.ToString());

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new AchievementNotificationRecord
        {
            Id = r.GetInt64(0),
            AccountId = accountId,
            Name = r.GetString(1),
            Description = r.GetString(2)
        };
    }

    public static void MarkDelivered(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = "UPDATE AchievementNotifications SET Delivered=1 WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        cmd.ExecuteNonQuery();
    }
}