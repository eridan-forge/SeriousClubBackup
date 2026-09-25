using System;

namespace серьёзный.Core.CoreShop;

public class ShopCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public int Order { get; set; }

    public bool Hidden { get; set; }
}