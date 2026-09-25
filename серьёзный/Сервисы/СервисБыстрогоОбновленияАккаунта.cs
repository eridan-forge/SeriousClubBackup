using серьёзный.Сервисы;

namespace серьёзный.Сервисы;

// Точечные SQL-обновления баланса аккаунта БЕЗ полной перезагрузки
// таблицы Accounts. new СервисАккаунтов() при каждом создании
// перечитывает и нормализует ВСЮ таблицу в конструкторе — это
// приемлемо для админ-окон (открываются редко), но было фатально
// дорого для таймера СервисСеансов, который дёргал это до двух раз
// в секунду НА КАЖДЫЙ активный сеанс, держа при этом lock всего
// сервиса сеансов на время SQL round-trip.
internal static class СервисБыстрогоОбновленияАккаунта
{
    private static readonly СервисБазы001 база = new();

    public static void УстановитьОстаток(Guid id, TimeSpan остаток)
    {
        using var db = база.Открыть();
        using var cmd = db.CreateCommand();

        cmd.CommandText =
            "UPDATE Accounts SET RemainingSeconds=@r WHERE Id=@id;";

        cmd.Parameters.AddWithValue(
            "@r",
            Math.Max(0L, (long)остаток.TotalSeconds));

        cmd.Parameters.AddWithValue("@id", id.ToString());

        cmd.ExecuteNonQuery();
    }

    // Атомарный инкремент прямо в SQL — не read-modify-write,
    // значит не нужно даже читать текущее значение.
    public static void ДобавитьСыгранное(Guid id, TimeSpan прошло)
    {
        if (прошло <= TimeSpan.Zero)
            return;

        using var db = база.Открыть();
        using var cmd = db.CreateCommand();

        cmd.CommandText =
            @"
UPDATE Accounts
SET PlayedSeconds = PlayedSeconds + @sec,
    LastSession = @now
WHERE Id=@id;";

        cmd.Parameters.AddWithValue("@sec", (long)прошло.TotalSeconds);
        cmd.Parameters.AddWithValue("@now", DateTime.Now.ToString("O"));
        cmd.Parameters.AddWithValue("@id", id.ToString());

        cmd.ExecuteNonQuery();
    }
}