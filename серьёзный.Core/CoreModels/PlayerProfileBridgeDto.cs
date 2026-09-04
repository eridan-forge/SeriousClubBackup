namespace серьёзный.Core.CoreModels;

public class PlayerProfileFrameDto
{
    public int Frame { get; set; }

    public bool Owned { get; set; }
}

public class PlayerAchievementDto
{
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public bool Unlocked { get; set; }

    public int? RewardFrame { get; set; }
}

public class PlayerProfileDto
{
    public Guid AccountId { get; set; }

    public string FullName { get; set; } = "";

    public long RemainingSeconds { get; set; }

    public long PlayedSeconds { get; set; }

    public int SessionCount { get; set; }

    public long Points { get; set; }

    public string LevelName { get; set; } = "";

    public double LevelMultiplierPercent { get; set; }

    public bool Premium { get; set; }

    public DateTime? PremiumUntil { get; set; }

    public int CurrentFrame { get; set; }

    public List<PlayerProfileFrameDto> Frames { get; set; } = new();

    public List<PlayerAchievementDto> Achievements { get; set; } = new();
}