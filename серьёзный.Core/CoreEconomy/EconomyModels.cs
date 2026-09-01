namespace серьёзный.Core.CoreEconomy;

public class PlayerPoints
{
    public Guid PlayerId { get; set; }

    public long Points { get; set; }

    public bool Premium { get; set; }

    public DateTime? PremiumUntil { get; set; }
}

public class PointsHistoryEntry
{
    public long Id { get; set; }

    public Guid PlayerId { get; set; }

    public long Delta { get; set; }

    public string Reason { get; set; } = "";

    public string? AdminName { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;

    public long BalanceAfter { get; set; }
}

public class LevelTier
{
    public int Level { get; set; }

    public string Name { get; set; } = "";

    // Порог по суммарно сыгранному времени (в секундах),
    // начиная с которого действует этот уровень.
    public long MinPlayedSeconds { get; set; }

    // 100 = x1.0, 150 = x1.5 и т.д. Начисление баллов
    // умножается на (MultiplierPercent / 100).
    public double MultiplierPercent { get; set; } = 100;
}

public class InventoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public string Icon { get; set; } = "🏆";

    // Надбавка к начислению баллов при экипировке предмета, в %.
    public double PointsBonusPercent { get; set; }

    // Надбавка к бонусному времени при покупке сеанса, в %.
    public double TimeBonusPercent { get; set; }

    public int PriceInPoints { get; set; }

    public bool Enabled { get; set; } = true;
}

public class PlayerInventoryEntry
{
    public Guid PlayerId { get; set; }

    public Guid ItemId { get; set; }

    public bool Equipped { get; set; }

    public DateTime AcquiredTime { get; set; } = DateTime.Now;
}

public class InventoryBonus
{
    public double PointsBonusPercent { get; set; }

    public double TimeBonusPercent { get; set; }
}

public class CasinoConfig
{
    public int MinBet { get; set; } = 10;

    public int MaxBet { get; set; } = 500;

    public double WinChancePercent { get; set; } = 45;

    public double WinMultiplier { get; set; } = 1.8;

    public bool Enabled { get; set; } = true;
}

public class CasinoResult
{
    public bool Win { get; set; }

    public long Bet { get; set; }

    public long Payout { get; set; }

    public long BalanceAfter { get; set; }
}

public class CaseInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "📦";

    public int PriceInPoints { get; set; } = 100;

    public bool Enabled { get; set; } = true;
}

public enum CaseRewardType
{
    Points,
    TimeMinutes,
    Item
}

public class CaseReward
{
    public long Id { get; set; }

    public Guid CaseId { get; set; }

    public CaseRewardType Type { get; set; }

    // Points/TimeMinutes: число строкой. Item: Guid предмета строкой.
    public string Value { get; set; } = "";

    public string Label { get; set; } = "";

    public int Weight { get; set; } = 1;
}

public class CaseOpenResult
{
    public string Label { get; set; } = "";

    public CaseRewardType Type { get; set; }

    public string Value { get; set; } = "";
}

public class EconomyConfig
{
    public double PointsPerMinutePurchased { get; set; } = 1.0;

    public int PointsPerAchievement { get; set; } = 50;

    public int PointsPerDrinkPurchase { get; set; } = 20;

    public double PremiumMultiplierBonusPercent { get; set; } = 20;
}

public class PurchaseRewardResult
{
    public long PointsAwarded { get; set; }

    public double MultiplierPercent { get; set; } = 100;

    public int BonusMinutes { get; set; }
}