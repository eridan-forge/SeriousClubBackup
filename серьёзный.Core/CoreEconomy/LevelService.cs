namespace серьёзный.Core.CoreEconomy;

public class LevelService
{
    private readonly EconomyConfigService config = new();

    public LevelTier GetTierByPlayedSeconds(long playedSeconds)
    {
        var tiers = config.GetTiers();

        if (tiers.Count == 0)
        {
            return new LevelTier
            {
                Level = 1,
                Name = "Новичок",
                MinPlayedSeconds = 0,
                MultiplierPercent = 100
            };
        }

        return tiers
            .Where(x => x.MinPlayedSeconds <= playedSeconds)
            .OrderByDescending(x => x.MinPlayedSeconds)
            .FirstOrDefault()
            ?? tiers.OrderBy(x => x.MinPlayedSeconds).First();
    }

    public double GetMultiplierPercent(long playedSeconds)
    {
        return GetTierByPlayedSeconds(playedSeconds).MultiplierPercent;
    }
}