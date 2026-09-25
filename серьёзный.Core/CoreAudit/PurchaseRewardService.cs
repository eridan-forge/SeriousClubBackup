namespace серьёзный.Core.CoreEconomy;

public class PurchaseRewardService
{
    private readonly EconomyConfigService config = new();
    private readonly LevelService levels = new();
    private readonly InventoryService inventory = new();
    private readonly PremiumService premium = new();
    private readonly PointsService points = new();

    public PurchaseRewardResult AwardForTimePurchase(
        Guid playerId,
        int minutesPurchased,
        long playedSeconds)
    {
        var economy = config.GetEconomy();

        var levelMultiplier = levels.GetMultiplierPercent(playedSeconds);

        var itemBonus = inventory.GetEquippedBonus(playerId);

        var isPremium = premium.IsPremium(playerId);

        var premiumBonus = isPremium ? economy.PremiumMultiplierBonusPercent : 0;

        var totalPercent = levelMultiplier + itemBonus.PointsBonusPercent + premiumBonus;

        var multiplier = totalPercent / 100.0;

        var basePoints = minutesPurchased * economy.PointsPerMinutePurchased;

        var awarded = (long)Math.Round(basePoints * multiplier);

        if (awarded > 0)
        {
            points.Award(
                playerId,
                awarded,
                $"Покупка времени ({minutesPurchased} мин, множитель x{multiplier:0.00})");
        }

        var bonusMinutes =
            (int)Math.Round(minutesPurchased * itemBonus.TimeBonusPercent / 100.0);

        return new PurchaseRewardResult
        {
            PointsAwarded = awarded,
            MultiplierPercent = totalPercent,
            BonusMinutes = bonusMinutes
        };
    }

    public void AwardForAchievement(Guid playerId, string achievementName)
    {
        var economy = config.GetEconomy();

        points.Award(playerId, economy.PointsPerAchievement, $"Достижение: {achievementName}");
    }

    public void AwardForDrinkPurchase(Guid playerId, string itemName)
    {
        var economy = config.GetEconomy();

        points.Award(playerId, economy.PointsPerDrinkPurchase, $"Покупка напитка: {itemName}");
    }
}