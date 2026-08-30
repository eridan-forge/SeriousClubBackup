using System.Text.Json;
using System.IO;

namespace серьёзный.Core.CoreSocial;

public enum FriendStatus
{
    Pending,
    Accepted,
    Blocked
}

public class FriendRelation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid From { get; set; }

    public Guid To { get; set; }

    public FriendStatus Status { get; set; }

    public DateTime Created { get; set; } = DateTime.Now;
}

public class OnlineState
{
    public Guid PlayerId { get; set; }

    public bool Online { get; set; }

    public int PcId { get; set; }

    public string? CurrentGame { get; set; }
}

public class SocialService
{
    private readonly string folder =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "Серьёзный",
            "Social");

    private readonly string friendsFile;
    private readonly string onlineFile;

    private readonly List<FriendRelation> friends = new();

    private readonly List<OnlineState> online = new();

    public SocialService()
    {
        Directory.CreateDirectory(folder);

        friendsFile =
            Path.Combine(folder, "friends.json");

        onlineFile =
            Path.Combine(folder, "online.json");

        Load();
    }

    public IReadOnlyList<FriendRelation> Friends =>
        friends;

    public IReadOnlyList<OnlineState> Online =>
        online;

    private void Load()
    {
        if (File.Exists(friendsFile))
        {
            var list =
                JsonSerializer.Deserialize<List<FriendRelation>>(
                    File.ReadAllText(friendsFile));

            if (list != null)
                friends.AddRange(list);
        }

        if (File.Exists(onlineFile))
        {
            var list =
                JsonSerializer.Deserialize<List<OnlineState>>(
                    File.ReadAllText(onlineFile));

            if (list != null)
                online.AddRange(list);
        }
    }

    private void SaveFriends()
    {
        File.WriteAllText(
            friendsFile,
            JsonSerializer.Serialize(
                friends,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }

    private void SaveOnline()
    {
        File.WriteAllText(
            onlineFile,
            JsonSerializer.Serialize(
                online,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }

    // ---------------- Друзья ----------------

    public bool IsFriend(Guid a, Guid b)
    {
        return friends.Any(x =>
            x.Status == FriendStatus.Accepted &&
            ((x.From == a && x.To == b) ||
             (x.From == b && x.To == a)));
    }

    public bool HasPending(Guid from, Guid to)
    {
        return friends.Any(x =>
            x.From == from &&
            x.To == to &&
            x.Status == FriendStatus.Pending);
    }

    public void SendRequest(Guid from, Guid to)
    {
        if (from == to)
            return;

        if (IsFriend(from, to))
            return;

        if (HasPending(from, to))
            return;

        friends.Add(
            new FriendRelation
            {
                From = from,
                To = to,
                Status = FriendStatus.Pending
            });

        SaveFriends();
    }

    public void Accept(Guid requestId)
    {
        var r =
            friends.FirstOrDefault(x => x.Id == requestId);

        if (r == null)
            return;

        r.Status = FriendStatus.Accepted;

        SaveFriends();
    }

    public void Remove(Guid a, Guid b)
    {
        friends.RemoveAll(x =>
            (x.From == a && x.To == b) ||
            (x.From == b && x.To == a));

        SaveFriends();
    }

    public void Block(Guid a, Guid b)
    {
        Remove(a, b);

        friends.Add(
            new FriendRelation
            {
                From = a,
                To = b,
                Status = FriendStatus.Blocked
            });

        SaveFriends();
    }

    public List<FriendRelation> Incoming(Guid player)
    {
        return friends
            .Where(x =>
                x.To == player &&
                x.Status == FriendStatus.Pending)
            .ToList();
    }

    // ---------------- Онлайн ----------------

    public void SetOnline(
        Guid player,
        int pc,
        string? game)
    {
        var s =
            online.FirstOrDefault(x =>
                x.PlayerId == player);

        if (s == null)
        {
            s = new OnlineState
            {
                PlayerId = player
            };

            online.Add(s);
        }

        s.Online = true;
        s.PcId = pc;
        s.CurrentGame = game;

        SaveOnline();
    }

    public void SetOffline(Guid player)
    {
        var s =
            online.FirstOrDefault(x =>
                x.PlayerId == player);

        if (s == null)
            return;

        s.Online = false;
        s.CurrentGame = null;

        SaveOnline();
    }

    public List<Guid> GetFriendIds(Guid player)
    {
        return friends
            .Where(x =>
                x.Status == FriendStatus.Accepted &&
                (x.From == player || x.To == player))
            .Select(x =>
                x.From == player ? x.To : x.From)
            .ToList();
    }

    public bool IsBlocked(Guid owner, Guid other)
    {
        return friends.Any(x =>
            x.Status == FriendStatus.Blocked &&
            x.From == owner &&
            x.To == other);
    }

    public void Unblock(Guid owner, Guid other)
    {
        friends.RemoveAll(x =>
            x.Status == FriendStatus.Blocked &&
            x.From == owner &&
            x.To == other);

        SaveFriends();
    }
}