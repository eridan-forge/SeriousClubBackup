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

    public string Image { get; set; } = "";

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