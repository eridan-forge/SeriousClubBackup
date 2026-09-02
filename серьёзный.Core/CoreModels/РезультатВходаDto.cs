namespace серьёзный.Core.CoreModels;

public class РезультатВходаDto
{
    public Guid AccountId { get; set; }

    public string FullName { get; set; } = "";

    public long RemainingSeconds { get; set; }
}