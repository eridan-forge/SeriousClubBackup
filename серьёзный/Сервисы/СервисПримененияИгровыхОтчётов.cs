using Microsoft.Data.Sqlite;

namespace серьёзный.Сервисы;

// Единственное место на сервере, которое применяет отчёт о сыгранном
// времени к балансу/статистике аккаунта. Идемпотентно: один и тот же
// отчёт (ПК + его локальный Id) применяется ровно один раз, даже если
// Патруль пришлёт его повторно после разрыва связи.
public class СервисПримененияИгровыхОтчётов
{
    private readonly СервисБазы001 база = new();

    // ВАЖНО: не храним долгоживущий экземпляр СервисАккаунтов - он
    // кэширует список аккаунтов в памяти только на момент создания
    // и не увидит аккаунт, созданный админом позже. По той же причине
    // СервисСеансов создаёт его заново на каждое обращение.
    private static СервисАккаунтов Аккаунты() => new();

    public СервисПримененияИгровыхОтчётов()
    {
        using var db = база.Открыть();
        using var cmd = db.CreateCommand();

        cmd.CommandText =
            @"
CREATE TABLE IF NOT EXISTS ProcessedSessionReports
(
    PcId INTEGER NOT NULL,
    ReportId INTEGER NOT NULL,
    Time TEXT NOT NULL,
    PRIMARY KEY(PcId, ReportId)
);";

        cmd.ExecuteNonQuery();
    }

    public void Применить(int pcId, int reportId, Guid accountId, long playedSeconds)
    {
        if (playedSeconds <= 0)
            return;

        using var db = база.Открыть();

        if (!ОтметитьВпервые(db, pcId, reportId))
            return; // уже применяли этот же отчёт раньше - не дублируем

        Аккаунты().ОбновитьВоВремяСеанса(
            accountId,
            TimeSpan.FromSeconds(playedSeconds),
            false);

        Аккаунты().ЗавершитьСтатистику(accountId);
    }

    private static bool ОтметитьВпервые(SqliteConnection db, int pcId, int reportId)
    {
        using var cmd = db.CreateCommand();

        cmd.CommandText =
            "INSERT OR IGNORE INTO ProcessedSessionReports(PcId, ReportId, Time) " +
            "VALUES($pc,$id,$t);";

        cmd.Parameters.AddWithValue("$pc", pcId);
        cmd.Parameters.AddWithValue("$id", reportId);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));

        return cmd.ExecuteNonQuery() > 0;
    }
}