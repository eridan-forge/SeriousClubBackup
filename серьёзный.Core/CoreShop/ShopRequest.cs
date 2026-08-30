using System;

namespace серьёзный.Core.CoreShop;

public class ShopRequest
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid AccountId { get; set; }

    public Guid ItemId { get; set; }

    public string ItemName { get; set; } = "";

    public decimal Price { get; set; }

    public int PcId { get; set; }

    public DateTime Time { get; set; } =
        DateTime.Now;

    public ShopRequestStatus Status { get; set; } =
        ShopRequestStatus.Pending;

    public ShopDeliveryType Delivery { get; set; } =
        ShopDeliveryType.ComeToAdmin;
}