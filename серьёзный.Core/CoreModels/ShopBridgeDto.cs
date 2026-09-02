namespace серьёзный.Core.CoreModels;

public class ShopPurchaseResultDto
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid RequestId { get; set; }
}

public class ShopOrderDto
{
    public Guid Id { get; set; }

    public string ItemName { get; set; } = "";

    public decimal Price { get; set; }

    public string Status { get; set; } = "";

    public string Delivery { get; set; } = "";

    public DateTime Time { get; set; }
}

public class ShopOrdersDto
{
    public List<ShopOrderDto> Orders { get; set; } = new();
}