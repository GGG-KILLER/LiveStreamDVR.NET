namespace LiveStreamDVR.Api.Models;

public sealed class TwitchGetStreamsResponse
{
    public required List<TwitchStream> Data { get; set; }

    public required TwitchResponsePagination Pagination { get; set; }
}

public sealed class TwitchStream
{
    public required string Id { get; set; }

    public required string UserId { get; set; }

    public required string UserLogin { get; set; }

    public required string UserName { get; set; }

    public required string GameId { get; set; }

    public required string GameName { get; set; }

    public required string Type { get; set; }

    public required string Title { get; set; }

    public required List<string> Tags { get; set; }

    public required long ViewerCount { get; set; }

    public required DateTimeOffset StartedAt { get; set; }

    public required string Language { get; set; }

    public required string ThumbnailUrl { get; set; }

    public required List<string> TagIds { get; set; }

    public required bool IsMature { get; set; }
}
