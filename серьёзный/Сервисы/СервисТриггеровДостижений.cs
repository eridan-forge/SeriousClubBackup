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
// Пороги (10 часов, 5 друзей, 50 сеансов) пока захардкожены — следующим
// шагом вынесем их в таблицу, редактируемую из админки, по тому же
// принципу, что и EconomyConfig/LevelTiers.
public class СервисТриггеровДостижений
{
    private readonly AchievementService достижения = new();
    private readonly SocialService social = new();
    private readonly ShopRequestService заказы = new();

    private const long ДесятьЧасовВСекундах = 10 * 3600;
    private const int ДрузейДляДостижения = 5;
    private const int СеансовДляВетерана = 50;

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

        if (аккаунт.ВсегоСыграно.TotalSeconds >= ДесятьЧасовВСекундах)
            достижения.Unlock(accountId, AchievementType.TenHours);

        if (аккаунт.ВсегоСеансов >= СеансовДляВетерана)
            достижения.Unlock(accountId, AchievementType.Veteran);

        if (social.GetFriendIds(accountId).Count >= ДрузейДляДостижения)
            достижения.Unlock(accountId, AchievementType.FiveFriends);

        if (заказы.All.Any(x => x.AccountId == accountId))
            достижения.Unlock(accountId, AchievementType.FirstPurchase);
    }
}