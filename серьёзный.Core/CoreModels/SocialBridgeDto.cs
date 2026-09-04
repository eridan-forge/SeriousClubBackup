namespace серьёзный.Core.CoreModels;

public enum SocialAction
{
    GetState,
    SendFriendRequest,
    AcceptFriendRequest,
    RemoveFriend,
    Block,
    Unblock
}

public class SocialActionDto
{
    public SocialAction Action { get; set; }

    public Guid TargetId { get; set; }

    public Guid RequestId { get; set; }
}

public class OnlinePlayerDto
{
    public Guid AccountId { get; set; }

    public string FullName { get; set; } = "";

    public bool Online { get; set; }

    public int PcId { get; set; }

    public string? CurrentGame { get; set; }

    public bool IsFriend { get; set; }

    public bool HasPendingOutgoing { get; set; }
}

public class IncomingFriendRequestDto
{
    public Guid RequestId { get; set; }

    public Guid FromAccountId { get; set; }

    public string FromFullName { get; set; } = "";
}

public class SocialStateDto
{
    public List<OnlinePlayerDto> Players { get; set; } = new();

    public List<IncomingFriendRequestDto> Incoming { get; set; } = new();
}