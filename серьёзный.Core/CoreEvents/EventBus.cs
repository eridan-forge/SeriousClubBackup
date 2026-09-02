namespace серьёзный.Core.CoreEvents;

// Единая точка публикации доменных событий. Любое окно/сервис может
// подписаться на события без прямой ссылки на источник — это убирает
// разрастание индивидуальных C# event'ов и десятки ручных таймеров.
public static class EventBus
{
    public static event Action<object>? Published;

    public static void Publish<T>(T payload) where T : notnull
    {
        Published?.Invoke(payload);
    }

    public static void Subscribe<T>(Action<T> handler) where T : notnull
    {
        Published += obj =>
        {
            if (obj is T typed)
                handler(typed);
        };
    }
}

// Конкретные события ядра. Добавлять новые сюда, а не плодить
// отдельные static event-классы по всему проекту.
public record SessionStartedEvent(int PcId, Guid? AccountId);

public record SessionEndedEvent(int PcId, Guid? AccountId);

public record ShopOrderStatusChangedEvent(Guid OrderId, string Status);

public record AchievementUnlockedEvent(Guid PlayerId, string AchievementName);

public record GameSessionReportedEvent(Guid AccountId, int PcId, long PlayedSeconds);