using System;
using System.Linq;
using серьёзный.Core.CoreEvents;
using серьёзный.Core.CoreProfiles;
using серьёзный.Core.CoreShop;
using серьёзный.Core.CoreSocial;

namespace серьёзный.Сервисы;

// Слушает игровые события через EventBus. Все они публикуются здесь же,
// в процессе сервера/админки — EventBus не переживает границу процесса,
// поэтому сервис намеренно не пытается слушать что-то, рождающееся на
// стороне ЭкранКлуба/Патруля на клиентских ПК.
//
// Пороги (10 часов, 5 друзей, 50 сеансов) хранятся в
// AchievementThresholdsService и правятся из админки без пересборки.
public class СервисТриггеровДостижений
{
    private readonly AchievementService достижения = new();
    private readonly SocialService social = new();
    private readonly ShopRequestService заказы = new();
    private readonly AchievementThresholdsService пороги = new();

    public void Инициализировать()
    {
        EventBus.Subscribe<SessionStartedEvent>(OnSessionStarted);
        EventBus.Subscribe<SessionEndedEvent>(OnSessionEnded);
        EventBus.Subscribe<GameSessionReportedEvent>(OnGameSessionReported);
    }

    private void OnSessionStarted(SessionStartedEvent e)
    {
        if (e.AccountId.HasValue)
            достижения.Unlock(e.AccountId.Value, AchievementType.FirstLogin);
    }

    private void OnSessionEnded(SessionEndedEvent e)
    {
        if (e.AccountId.HasValue)
            ПроверитьПоАккаунту(e.AccountId.Value);
    }

    private void OnGameSessionReported(GameSessionReportedEvent e)
    {
        ПроверитьПоАккаунту(e.AccountId);
    }

    private void ПроверитьПоАккаунту(Guid accountId)
    {
        var аккаунт = new СервисАккаунтов().Получить(accountId);

        if (аккаунт == null)
            return;

        var текущиеПороги = пороги.Get();

        if (аккаунт.ВсегоСыграно.TotalSeconds >= текущиеПороги.TenHoursSeconds)
            достижения.Unlock(accountId, AchievementType.TenHours);

        if (аккаунт.ВсегоСеансов >= текущиеПороги.VeteranSessionsCount)
            достижения.Unlock(accountId, AchievementType.Veteran);

        if (social.GetFriendIds(accountId).Count >= текущиеПороги.FiveFriendsCount)
            достижения.Unlock(accountId, AchievementType.FiveFriends);

        if (заказы.All.Any(x => x.AccountId == accountId))
            достижения.Unlock(accountId, AchievementType.FirstPurchase);
    }
}