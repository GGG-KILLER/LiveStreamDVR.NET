namespace LiveStreamDVR.Api.Models;

public sealed class TwitchGetUsersResponse
{
    public required List<TwitchUser> Data { get; set; }
}

public sealed class TwitchUser
{
    public required string Id { get; set; }

    public required string Login { get; set; }

    public required string DisplayName { get; set; }

    public required string Type { get; set; }

    public required string BroadcasterType { get; set; }

    public required string Description { get; set; }

    public required Uri ProfileImageUrl { get; set; }

    public required Uri OfflineImageUrl { get; set; }

    public required long ViewCount { get; set; }

    public string? Email { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }
}
