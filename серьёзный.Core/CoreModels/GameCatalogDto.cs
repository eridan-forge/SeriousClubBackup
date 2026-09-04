namespace серьёзный.Core.CoreModels;

public class GameCatalogItemDto
{
    public Guid Id { get; set; }

    public string Название { get; set; } = "";

    public string Категория { get; set; } = "";

    public string Описание { get; set; } = "";

    public string Путь { get; set; } = "";

    public string Обложка { get; set; } = "";

    public int Порядок { get; set; }
}

public class GameCatalogDto
{
    public List<GameCatalogItemDto> Games { get; set; } = new();
}