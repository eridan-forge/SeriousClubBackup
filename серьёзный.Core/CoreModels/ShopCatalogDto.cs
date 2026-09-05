namespace серьёзный.Core.CoreModels;

public class ShopCategoryDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public int Order { get; set; }
}

public class ShopItemDto
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public decimal Price { get; set; }

    // Путь на диске АДМИНСКОГО ПК — оставлен для отладки. Клиент для
    // отображения обязан использовать ImageData (см. ImageCacheService),
    // этот путь на клиентской машине не существует.
    public string Image { get; set; } = "";

    public string? ImageData { get; set; }

    public string? ImageExtension { get; set; }

    public bool Featured { get; set; }

    public bool IsNew { get; set; }

    public int Stock { get; set; }
}

public class ShopCatalogDto
{
    public bool Enabled { get; set; }

    public List<ShopCategoryDto> Categories { get; set; } = new();

    public List<ShopItemDto> Items { get; set; } = new();
}