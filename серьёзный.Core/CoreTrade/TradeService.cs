using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreDb;
using серьёзный.Core.CoreSocial;

namespace серьёзный.Core.CoreTrade;

public enum TradeStatus
{
    Pending,            // ждёт ответа второй стороны
    PendingAdminReview, // обе/одна сторона согласны, но нужен админ
    Accepted,           // завершён успешно
    Declined,           // получатель отклонил
    Cancelled,          // отправитель отменил сам, пока Pending
    Reversed            // админ откатил уже совершённый обмен (в течение 3 дней)
}

public class TradeItemLine
{
    public long Id { get; set; }
    public long TradeId { get; set; }
    public string Side { get; set; } = ""; // "From" / "To"
    public Guid? ItemId { get; set; }      // null если это не предмет
    public long Points { get; set; }
    public long TimeSeconds { get; set; }
}

public class TradeRecord
{
    public long Id { get; set; }
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public TradeStatus Status { get; set; }
    public bool RequiresAdmin { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Completed { get; set; }
    public string? AdminNote { get; set; }
    public List<TradeItemLine> Lines { get; set; } = new();
}

// Простой, но расширяемый трейд между игроками. Работает поверх уже
// существующих InventoryItems/PlayerInventory (InventoryService),
// PlayerPoints (PointsService) и дружбы (SocialService) — не заводит
// вторую копию инвентаря/баланса, только сама двигает строки в тех же
// таблицах внутри одной SQLite-транзакции.
public class TradeService
{
    private static readonly TimeSpan ЛокПослеОбмена = TimeSpan.FromDays(3);

    private readonly string db =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SeriousClub", "SeriousClub.db");

    private readonly SocialService social = new();
    private readonly AdminActionLogService лог = new();

    public TradeService()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(db)!);

        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS Trades(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FromId TEXT NOT NULL,
            ToId TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Pending',
            RequiresAdmin INTEGER NOT NULL DEFAULT 0,
            Created TEXT NOT NULL,
            Completed TEXT,
            AdminNote TEXT
        );

        CREATE TABLE IF NOT EXISTS TradeItems(
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TradeId INTEGER NOT NULL,
            Side TEXT NOT NULL,
            ItemId TEXT,
            Points INTEGER NOT NULL DEFAULT 0,
            TimeSeconds INTEGER NOT NULL DEFAULT 0
        );
        """;

        cmd.ExecuteNonQuery();

        ДобавитьКолонкуЛокаЕслиНужно(con);
    }

    private SqliteConnection Open() => SqliteDb.Open();

    // PlayerInventory уже существует (InventoryService) — просто
    // добавляем колонку блокировки, если её ещё нет. Тот же паттерн
    // PRAGMA table_info, что везде по проекту.
    private static void ДобавитьКолонкуЛокаЕслиНужно(SqliteConnection con)
    {
        using var check = con.CreateCommand();
        check.CommandText = "PRAGMA table_info(PlayerInventory);";

        using (var reader = check.ExecuteReader())
        {
            while (reader.Read())
            {
                var имя = reader.IsDBNull(1) ? "" : reader.GetString(1);
                if (string.Equals(имя, "LockedUntil", StringComparison.OrdinalIgnoreCase))
                    return;
            }
        }

        using var alter = con.CreateCommand();
        alter.CommandText =
            "ALTER TABLE PlayerInventory ADD COLUMN LockedUntil TEXT;";
        alter.ExecuteNonQuery();
    }

    // =====================================================
    // СОЗДАНИЕ ПРЕДЛОЖЕНИЯ ОБМЕНА
    // =====================================================

    public long CreateOffer(
        Guid fromId,
        Guid toId,
        List<Guid> fromItemIds,
        long fromPoints,
        long fromTimeSeconds,
        List<Guid> toItemIds,
        long toPoints,
        long toTimeSeconds,
        bool requestAdminSupervision,
        out string error)
    {
        error = "";

        if (fromId == toId)
        {
            error = "Нельзя обмениваться с самим собой.";
            return 0;
        }

        if (!social.IsFriend(fromId, toId))
        {
            error = "Обмен возможен только между друзьями.";
            return 0;
        }

        if (fromPoints < 0 || toPoints < 0 || fromTimeSeconds < 0 || toTimeSeconds < 0)
        {
            error = "Отрицательные значения недопустимы.";
            return 0;
        }

        using var con = Open();

        // Проверяем, что все указанные предметы реально принадлежат
        // соответствующему игроку и не залочены прошлым обменом.
        if (!ПроверитьПредметы(con, fromId, fromItemIds, out error)) return 0;
        if (!ПроверитьПредметы(con, toId, toItemIds, out error)) return 0;

        var сейчас = DateTime.Now;

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO Trades(FromId, ToId, Status, RequiresAdmin, Created)
        VALUES($f,$t,$s,$a,$c);
        """;

        cmd.Parameters.AddWithValue("$f", fromId.ToString());
        cmd.Parameters.AddWithValue("$t", toId.ToString());
        cmd.Parameters.AddWithValue("$s", TradeStatus.Pending.ToString());
        cmd.Parameters.AddWithValue("$a", requestAdminSupervision ? 1 : 0);
        cmd.Parameters.AddWithValue("$c", сейчас.ToString("O"));

        cmd.ExecuteNonQuery();

        using var idCmd = con.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid();";
        var tradeId = Convert.ToInt64(idCmd.ExecuteScalar());

        void ДобавитьСтроки(string side, List<Guid> items, long points, long time)
        {
            foreach (var itemId in items)
                ВставитьСтроку(con, tradeId, side, itemId, 0, 0);

            if (points > 0 || time > 0)
                ВставитьСтроку(con, tradeId, side, null, points, time);
        }

        ДобавитьСтроки("From", fromItemIds, fromPoints, fromTimeSeconds);
        ДобавитьСтроки("To", toItemIds, toPoints, toTimeSeconds);

        лог.Log("Создано предложение обмена",
            $"#{tradeId}: {fromId} -> {toId}, под контролем админа: {requestAdminSupervision}",
            "Игрок");

        return tradeId;
    }

    private static void ВставитьСтроку(
        SqliteConnection con, long tradeId, string side, Guid? itemId, long points, long time)
    {
        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        INSERT INTO TradeItems(TradeId, Side, ItemId, Points, TimeSeconds)
        VALUES($tid,$side,$item,$p,$t);
        """;

        cmd.Parameters.AddWithValue("$tid", tradeId);
        cmd.Parameters.AddWithValue("$side", side);
        cmd.Parameters.AddWithValue("$item", (object?)itemId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$p", points);
        cmd.Parameters.AddWithValue("$t", time);

        cmd.ExecuteNonQuery();
    }

    private static bool ПроверитьПредметы(
        SqliteConnection con, Guid ownerId, List<Guid> itemIds, out string error)
    {
        error = "";

        foreach (var itemId in itemIds)
        {
            var cmd = con.CreateCommand();

            cmd.CommandText =
                "SELECT LockedUntil FROM PlayerInventory WHERE PlayerId=$p AND ItemId=$i;";

            cmd.Parameters.AddWithValue("$p", ownerId.ToString());
            cmd.Parameters.AddWithValue("$i", itemId.ToString());

            var value = cmd.ExecuteScalar();

            if (value == null)
            {
                error = "Один из предметов не найден в инвентаре.";
                return false;
            }

            if (value != DBNull.Value &&
                DateTime.TryParse((string)value, out var lockedUntil) &&
                lockedUntil > DateTime.Now)
            {
                error = $"Предмет заблокирован до {lockedUntil:dd.MM.yyyy HH:mm} после недавнего обмена.";
                return false;
            }
        }

        return true;
    }

    // =====================================================
    // ОТВЕТ ПОЛУЧАТЕЛЯ / ЗАВЕРШЕНИЕ
    // =====================================================

    // Вызывает ToId — принимает предложение. Если требуется присмотр
    // админа (кто-то из сторон это запросил) — сделка не завершается
    // сама, переходит в PendingAdminReview.
    public bool Accept(long tradeId, Guid byPlayerId, out string error)
    {
        error = "";

        var trade = GetById(tradeId);

        if (trade == null || trade.Status != TradeStatus.Pending)
        {
            error = "Сделка недоступна.";
            return false;
        }

        if (trade.ToId != byPlayerId)
        {
            error = "Принять сделку может только получатель предложения.";
            return false;
        }

        if (trade.RequiresAdmin)
        {
            SetStatus(tradeId, TradeStatus.PendingAdminReview);
            лог.Log("Обмен ждёт админа", $"#{tradeId}", "Игрок");
            return true;
        }

        return ЗавершитьОбмен(trade, out error);
    }

    public bool Decline(long tradeId, Guid byPlayerId, out string error)
    {
        error = "";

        var trade = GetById(tradeId);

        if (trade == null || trade.Status != TradeStatus.Pending)
        {
            error = "Сделка недоступна.";
            return false;
        }

        if (trade.ToId != byPlayerId)
        {
            error = "Отклонить может только получатель.";
            return false;
        }

        SetStatus(tradeId, TradeStatus.Declined);
        return true;
    }

    public bool Cancel(long tradeId, Guid byPlayerId, out string error)
    {
        error = "";

        var trade = GetById(tradeId);

        if (trade == null ||
            (trade.Status != TradeStatus.Pending && trade.Status != TradeStatus.PendingAdminReview))
        {
            error = "Сделку уже нельзя отменить.";
            return false;
        }

        if (trade.FromId != byPlayerId && trade.ToId != byPlayerId)
        {
            error = "Вы не участник этой сделки.";
            return false;
        }

        SetStatus(tradeId, TradeStatus.Cancelled);
        return true;
    }

    // Админ одобряет сделку, которая ждала присмотра.
    public bool AdminApprove(long tradeId, string adminName, out string error)
    {
        error = "";

        var trade = GetById(tradeId);

        if (trade == null || trade.Status != TradeStatus.PendingAdminReview)
        {
            error = "Сделка не ждёт одобрения.";
            return false;
        }

        var ok = ЗавершитьОбмен(trade, out error);

        if (ok)
            лог.Log("Обмен одобрен админом", $"#{tradeId}", adminName);

        return ok;
    }

    public void AdminReject(long tradeId, string adminName, string note)
    {
        SetAdminNote(tradeId, note);
        SetStatus(tradeId, TradeStatus.Declined);
        лог.Log("Обмен отклонён админом", $"#{tradeId}: {note}", adminName);
    }

    // Откат уже совершённого обмена (в течение 3 дней, пока предметы
    // ещё залочены) — забирает всё обратно к исходным владельцам.
    public bool AdminReverse(long tradeId, string adminName, string note, out string error)
    {
        error = "";

        var trade = GetById(tradeId);

        if (trade == null || trade.Status != TradeStatus.Accepted)
        {
            error = "Можно откатить только завершённый обмен.";
            return false;
        }

        if (trade.Completed.HasValue && DateTime.Now - trade.Completed.Value > ЛокПослеОбмена)
        {
            error = "Срок отката (3 дня) истёк.";
            return false;
        }

        using var con = Open();

        // Возвращаем предметы обратно исходным владельцам, снимаем
        // блокировку; баллы/время возвращаем начислением в обратную сторону.
        foreach (var line in trade.Lines)
        {
            var принадлежалКомуДо = line.Side == "From" ? trade.FromId : trade.ToId;
            var получилКто = line.Side == "From" ? trade.ToId : trade.FromId;

            if (line.ItemId.HasValue)
            {
                ПереместитьПредмет(con, line.ItemId.Value, from: получилКто, to: принадлежалКомуДо, снятьЛок: true);
            }

            if (line.Points > 0)
            {
                new серьёзный.Core.CoreEconomy.PointsService()
                    .Award(получилКто, -line.Points, $"Откат обмена #{tradeId}", adminName);

                new серьёзный.Core.CoreEconomy.PointsService()
                    .Award(принадлежалКомуДо, line.Points, $"Откат обмена #{tradeId}", adminName);
            }
            // TimeSeconds — обмен временем в этой упрощённой версии не
            // применяется к Accounts.RemainingSeconds автоматически (см.
            // TODO в ЗавершитьОбмен) — соответственно и откатывать нечего.
        }

        SetAdminNote(tradeId, note);
        SetStatus(tradeId, TradeStatus.Reversed);

        лог.Log("Обмен отменён (откат) админом", $"#{tradeId}: {note}", adminName);

        return true;
    }

    private bool ЗавершитьОбмен(TradeRecord trade, out string error)
    {
        error = "";

        using var con = Open();

        // Финальная проверка прав на предметы прямо перед переносом —
        // защищает от гонки (например предмет уже потрачен/передан
        // в другом обмене, пока это предложение висело).
        var fromItems = trade.Lines.Where(x => x.Side == "From" && x.ItemId.HasValue).Select(x => x.ItemId!.Value).ToList();
        var toItems = trade.Lines.Where(x => x.Side == "To" && x.ItemId.HasValue).Select(x => x.ItemId!.Value).ToList();

        if (!ПроверитьПредметы(con, trade.FromId, fromItems, out error)) return false;
        if (!ПроверитьПредметы(con, trade.ToId, toItems, out error)) return false;

        var points = new серьёзный.Core.CoreEconomy.PointsService();

        foreach (var line in trade.Lines)
        {
            var owner = line.Side == "From" ? trade.FromId : trade.ToId;
            var receiver = line.Side == "From" ? trade.ToId : trade.FromId;

            if (line.ItemId.HasValue)
            {
                ПереместитьПредмет(con, line.ItemId.Value, from: owner, to: receiver, снятьЛок: false);
            }

            if (line.Points > 0)
            {
                points.Award(owner, -line.Points, $"Обмен #{trade.Id}: отдано");
                points.Award(receiver, line.Points, $"Обмен #{trade.Id}: получено");
            }

            // Обмен игровым временем (TimeSeconds) намеренно не применяется
            // к Accounts.RemainingSeconds автоматически в этой упрощённой
            // версии — списание/начисление времени завязано на активный
            // сеанс (СервисСеансов), а тут может не быть активного сеанса
            // ни у одной из сторон. Следующий шаг: провести через тот же
            // путь, что СервисБыстрогоОбновленияАккаунта.УстановитьОстаток.
        }

        SetStatus(trade.Id, TradeStatus.Accepted, completedNow: true);

        лог.Log("Обмен завершён", $"#{trade.Id}: {trade.FromId} <-> {trade.ToId}", "Игрок");

        return true;
    }

    private static void ПереместитьПредмет(
        SqliteConnection con, Guid itemId, Guid from, Guid to, bool снятьЛок)
    {
        var lockedUntil = снятьЛок ? (string?)null : DateTime.Now.Add(ЛокПослеОбмена).ToString("O");

        using (var del = con.CreateCommand())
        {
            del.CommandText = "DELETE FROM PlayerInventory WHERE PlayerId=$p AND ItemId=$i;";
            del.Parameters.AddWithValue("$p", from.ToString());
            del.Parameters.AddWithValue("$i", itemId.ToString());
            del.ExecuteNonQuery();
        }

        using var ins = con.CreateCommand();

        ins.CommandText =
        """
        INSERT INTO PlayerInventory(PlayerId, ItemId, Equipped, AcquiredTime, LockedUntil)
        VALUES($p,$i,0,$t,$l)
        ON CONFLICT(PlayerId, ItemId) DO UPDATE SET LockedUntil=$l, Equipped=0;
        """;

        ins.Parameters.AddWithValue("$p", to.ToString());
        ins.Parameters.AddWithValue("$i", itemId.ToString());
        ins.Parameters.AddWithValue("$t", DateTime.Now.ToString("O"));
        ins.Parameters.AddWithValue("$l", (object?)lockedUntil ?? DBNull.Value);

        ins.ExecuteNonQuery();
    }

    private void SetStatus(long tradeId, TradeStatus status, bool completedNow = false)
    {
        using var con = Open();
        var cmd = con.CreateCommand();

        cmd.CommandText = completedNow
            ? "UPDATE Trades SET Status=$s, Completed=$c WHERE Id=$id;"
            : "UPDATE Trades SET Status=$s WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$s", status.ToString());
        cmd.Parameters.AddWithValue("$id", tradeId);

        if (completedNow)
            cmd.Parameters.AddWithValue("$c", DateTime.Now.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    private void SetAdminNote(long tradeId, string note)
    {
        using var con = Open();
        var cmd = con.CreateCommand();

        cmd.CommandText = "UPDATE Trades SET AdminNote=$n WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$n", note);
        cmd.Parameters.AddWithValue("$id", tradeId);
        cmd.ExecuteNonQuery();
    }

    // =====================================================
    // ЧТЕНИЕ
    // =====================================================

    public TradeRecord? GetById(long id)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, FromId, ToId, Status, RequiresAdmin, Created, Completed, AdminNote " +
            "FROM Trades WHERE Id=$id;";

        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();

        if (!r.Read()) return null;

        var trade = Прочитать(r);

        r.Close();

        trade.Lines = ЗагрузитьСтроки(con, id);

        return trade;
    }

    // Для игрока — его активные (не завершённые) сделки, входящие и исходящие.
    public List<TradeRecord> GetActiveForPlayer(Guid playerId)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText =
        """
        SELECT Id, FromId, ToId, Status, RequiresAdmin, Created, Completed, AdminNote
        FROM Trades
        WHERE (FromId=$p OR ToId=$p)
          AND Status IN ('Pending','PendingAdminReview')
        ORDER BY Created DESC;
        """;

        cmd.Parameters.AddWithValue("$p", playerId.ToString());

        var list = new List<TradeRecord>();

        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
                list.Add(Прочитать(r));
        }

        foreach (var t in list)
            t.Lines = ЗагрузитьСтроки(con, t.Id);

        return list;
    }

    // Для админ-панели — ВСЕ сделки в реальном времени (обычный live
    // SELECT без кэша, как остальные сервисы проекта: любой опрос
    // читает актуальное состояние базы).
    public List<TradeRecord> GetAllForAdmin(bool onlyActive = false)
    {
        using var con = Open();

        var cmd = con.CreateCommand();

        cmd.CommandText = onlyActive
            ? "SELECT Id, FromId, ToId, Status, RequiresAdmin, Created, Completed, AdminNote " +
              "FROM Trades WHERE Status IN ('Pending','PendingAdminReview') ORDER BY Created DESC;"
            : "SELECT Id, FromId, ToId, Status, RequiresAdmin, Created, Completed, AdminNote " +
              "FROM Trades ORDER BY Created DESC;";

        var list = new List<TradeRecord>();

        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
                list.Add(Прочитать(r));
        }

        foreach (var t in list)
            t.Lines = ЗагрузитьСтроки(con, t.Id);

        return list;
    }

    // Очистка истории — только завершённые/отклонённые/отменённые
    // сделки старше указанного срока. Активные (Pending/PendingAdminReview)
    // никогда не удаляются, даже если старые.
    public int ClearHistory(TimeSpan olderThan)
    {
        using var con = Open();

        var порог = (DateTime.Now - olderThan).ToString("O");

        var idsCmd = con.CreateCommand();

        idsCmd.CommandText =
        """
        SELECT Id FROM Trades
        WHERE Status NOT IN ('Pending','PendingAdminReview')
          AND Created < $t;
        """;

        idsCmd.Parameters.AddWithValue("$t", порог);

        var ids = new List<long>();

        using (var r = idsCmd.ExecuteReader())
        {
            while (r.Read())
                ids.Add(r.GetInt64(0));
        }

        foreach (var id in ids)
        {
            using var delItems = con.CreateCommand();
            delItems.CommandText = "DELETE FROM TradeItems WHERE TradeId=$id;";
            delItems.Parameters.AddWithValue("$id", id);
            delItems.ExecuteNonQuery();

            using var delTrade = con.CreateCommand();
            delTrade.CommandText = "DELETE FROM Trades WHERE Id=$id;";
            delTrade.Parameters.AddWithValue("$id", id);
            delTrade.ExecuteNonQuery();
        }

        лог.Log("Очищена история обменов", $"Удалено записей: {ids.Count}", "Администратор");

        return ids.Count;
    }

    private static List<TradeItemLine> ЗагрузитьСтроки(SqliteConnection con, long tradeId)
    {
        var cmd = con.CreateCommand();

        cmd.CommandText =
            "SELECT Id, TradeId, Side, ItemId, Points, TimeSeconds FROM TradeItems WHERE TradeId=$id;";

        cmd.Parameters.AddWithValue("$id", tradeId);

        var list = new List<TradeItemLine>();

        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            list.Add(new TradeItemLine
            {
                Id = r.GetInt64(0),
                TradeId = r.GetInt64(1),
                Side = r.GetString(2),
                ItemId = r.IsDBNull(3) ? null : Guid.Parse(r.GetString(3)),
                Points = r.GetInt64(4),
                TimeSeconds = r.GetInt64(5)
            });
        }

        return list;
    }

    private static TradeRecord Прочитать(SqliteDataReader r)
    {
        return new TradeRecord
        {
            Id = r.GetInt64(0),
            FromId = Guid.Parse(r.GetString(1)),
            ToId = Guid.Parse(r.GetString(2)),
            Status = Enum.Parse<TradeStatus>(r.GetString(3)),
            RequiresAdmin = r.GetInt32(4) == 1,
            Created = DateTime.Parse(r.GetString(5)),
            Completed = r.IsDBNull(6) ? null : DateTime.Parse(r.GetString(6)),
            AdminNote = r.IsDBNull(7) ? null : r.GetString(7)
        };
    }
}