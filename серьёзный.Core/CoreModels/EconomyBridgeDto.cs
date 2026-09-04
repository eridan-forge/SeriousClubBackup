namespace серьёзный.Core.CoreModels;

public enum EconomyAction
{
    GetSummary,
    PlayCasino,
    OpenCase,
    SetEquipped
}

public class EconomyRequestDto
{
    public EconomyAction Action { get; set; }

    public long Bet { get; set; }        // PlayCasino

    public Guid CaseId { get; set; }      // OpenCase

    public Guid ItemId { get; set; }      // SetEquipped

    public bool Equipped { get; set; }    // SetEquipped
}

public class InventoryItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "🏆";

    public double PointsBonusPercent { get; set; }

    public double TimeBonusPercent { get; set; }

    public int PriceInPoints { get; set; }

    public bool Owned { get; set; }

    public bool Equipped { get; set; }
}

public class CaseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Icon { get; set; } = "📦";

    public int PriceInPoints { get; set; }
}

public class EconomySummaryDto
{
    public long Points { get; set; }

    public bool Premium { get; set; }

    public List<InventoryItemDto> Inventory { get; set; } = new();

    public List<CaseDto> Cases { get; set; } = new();

    public int CasinoMinBet { get; set; }

    public int CasinoMaxBet { get; set; }

    public bool CasinoEnabled { get; set; }
}

public class EconomyResultDto
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public EconomySummaryDto? Summary { get; set; }

    public bool Win { get; set; }

    public long Payout { get; set; }

    public string? RewardLabel { get; set; }
}