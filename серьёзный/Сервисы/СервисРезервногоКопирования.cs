using System;
using серьёзный.Core.CoreBackup;

namespace серьёзный.Сервисы;

public sealed class СервисРезервногоКопирования : IDisposable
{
    private readonly System.Timers.Timer таймер;

    private readonly DatabaseBackupService резерв = new();

    public СервисРезервногоКопирования()
    {
        // Каждые 6 часов достаточно для клуба на 5 ПК: при аварии
        // теряется максимум несколько последних часов истории.
        таймер = new System.Timers.Timer(TimeSpan.FromHours(6).TotalMilliseconds);

        таймер.Elapsed += (_, _) => резерв.СоздатьРезервнуюКопию();
    }

    public void Запустить()
    {
        // Первая копия — сразу при старте сервера, не через 6 часов.
        резерв.СоздатьРезервнуюКопию();

        таймер.Start();
    }

    public void Остановить()
    {
        таймер.Stop();
    }

    public void Dispose()
    {
        таймер.Dispose();
    }
}