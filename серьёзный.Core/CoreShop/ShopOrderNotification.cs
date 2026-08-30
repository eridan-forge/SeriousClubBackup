namespace серьёзный.Core.CoreShop;

public class ShopOrderNotification
{
    public Guid OrderId { get; set; }

    public Guid PlayerId { get; set; }

    public string PlayerName { get; set; } = "";

    public string PcName { get; set; } = "";

    public string ItemName { get; set; } = "";

    public decimal Price { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;
}