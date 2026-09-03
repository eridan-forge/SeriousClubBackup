namespace серьёзный.Core.CoreModels;

public class ChatMessageDto
{
    public string Имя { get; set; } = "";

    public string Текст { get; set; } = "";

    public DateTime Время { get; set; }

    public bool ОтАдминистратора { get; set; }
}

public class ChatHistoryDto
{
    public List<ChatMessageDto> Сообщения { get; set; } = new();
}