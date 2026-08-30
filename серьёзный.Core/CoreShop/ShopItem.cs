using System;

namespace серьёзный.Core.CoreShop;

public class ShopItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public decimal Price { get; set; }

    public string Image { get; set; } = "";

    public bool Hidden { get; set; }

    public bool Featured { get; set; }

    public bool IsNew { get; set; }

    public int Stock { get; set; }
}