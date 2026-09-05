namespace серьёзный.Core.CoreModels;

public class GameCatalogItemDto
{
    public Guid Id { get; set; }

    public string Название { get; set; } = "";

    public string Категория { get; set; } = "";

    public string Описание { get; set; } = "";

    public string Путь { get; set; } = "";

    // Путь на диске АДМИНСКОГО ПК — см. ОбложкаData для клиента.
    public string Обложка { get; set; } = "";

    public string? ОбложкаData { get; set; }

    public string? ОбложкаExtension { get; set; }

    public int Порядок { get; set; }

    public string AppId { get; set; } = "";

    public string Launcher { get; set; } = "";
}

public class GameCatalogDto
{
    public List<GameCatalogItemDto> Games { get; set; } = new();
}