namespace серьёзный.Core.CoreModels;

public class PlayerChatMessageDto
{
    public Guid From { get; set; }

    public Guid To { get; set; }

    public string FromName { get; set; } = "";

    public string Text { get; set; } = "";

    public DateTime Time { get; set; }
}

public class PlayerChatHistoryDto
{
    public List<PlayerChatMessageDto> Messages { get; set; } = new();
}