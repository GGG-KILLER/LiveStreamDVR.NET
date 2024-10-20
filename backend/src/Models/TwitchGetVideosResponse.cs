namespace LiveStreamDVR.Api.Models;

public sealed class TwitchGetVideosResponse
{
    public required List<TwitchVideo> Data { get; set; }

    public required TwitchResponsePagination Pagination { get; set; }
}

public sealed class TwitchVideo
{
    public required string Id { get; set; }

    public required string? StreamId { get; set; }

    public required string UserId { get; set; }

    public required string UserLogin { get; set; }

    public required string UserName { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public required DateTimeOffset PublishedAt { get; set; }

    public required Uri Url { get; set; }

    public required string ThumbnailUrl { get; set; }

    public required string Viewable { get; set; }

    public required long ViewCount { get; set; }

    public required string Language { get; set; }

    public required string Type { get; set; }

    public required string Duration { get; set; }

    public required List<TwitchVideoMutedSegment>? MutedSegments { get; set; }
}

public sealed class TwitchVideoMutedSegment
{
    public required long Duration { get; set; }

    public required long Offset { get; set; }
}
