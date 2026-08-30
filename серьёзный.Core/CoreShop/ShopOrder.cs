using System;

namespace серьёзный.Core.CoreShop;

public class ShopOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ItemId { get; set; }

    public Guid PlayerId { get; set; }

    public string PlayerName { get; set; } = "";

    public string PcName { get; set; } = "";

    public DateTime Time { get; set; } = DateTime.Now;

    public bool Completed { get; set; }
}