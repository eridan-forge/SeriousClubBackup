using System.Text.Json;
using System.IO;
using серьёзный.Core.CoreEvents;

namespace серьёзный.Core.CoreShop;

public class ShopRequestService
{
    private readonly string file;


    private readonly List<ShopRequest> requests =
        new();

    public ShopRequestService()
    {
        file =
            Path.Combine(
                ShopPaths.Root,
                "requests.json");

        Load();
    }

    public IReadOnlyList<ShopRequest> All =>
        requests;


    public ShopRequest Create(
    Guid accountId,
    int pcId,
    Guid itemId,
    string itemName,
    decimal price,
    ShopDeliveryType delivery)
    {
        var request = new ShopRequest
        {
            Id = Guid.NewGuid(),

            AccountId = accountId,

            PcId = pcId,

            ItemId = itemId,

            ItemName = itemName,

            Price = price,

            Delivery = delivery,

            Time = DateTime.Now,

            Status = ShopRequestStatus.Pending
        };

        requests.Add(request);

        Save();

        ShopRequestEvent.Notify(request);

        return request;
    }


    private void Load()
    {
        Directory.CreateDirectory(ShopPaths.Root);

        if (!File.Exists(file))
            return;

        var list =
            JsonSerializer.Deserialize<List<ShopRequest>>(
                File.ReadAllText(file));

        if (list != null)
            requests.AddRange(list);
    }

    private void Save()
    {
        File.WriteAllText(
            file,
            JsonSerializer.Serialize(
                requests,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }

    public void Complete(Guid requestId)
    {
        var request =
            requests.FirstOrDefault(
                x => x.Id == requestId);

        if (request == null)
            return;

        request.Status =
            ShopRequestStatus.Completed;

        Save();

        ShopRequestCompletedEvent.Notify(request);
    }

    public void SetPreparing(Guid id)
    {
        SetStatus(id, ShopRequestStatus.Preparing);
    }

    public void SetReady(Guid id)
    {
        SetStatus(id, ShopRequestStatus.Ready);
    }

    public void SetCompleted(Guid id)
    {
        SetStatus(id, ShopRequestStatus.Completed);
    }

    public void Cancel(Guid id)
    {
        SetStatus(id, ShopRequestStatus.Cancelled);
    }

    private void SetStatus(Guid id, ShopRequestStatus status)
    {
        var request =
            requests.FirstOrDefault(x => x.Id == id);

        if (request == null)
            return;

        request.Status = status;

        Save();

        ShopLiveEvents.NotifyUpdated(request);
    }
}