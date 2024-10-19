using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

public partial class TwitchGetVideosResponse
{
    [JsonPropertyName("data")]
    public required List<TwitchVideo> Data { get; set; }

    [JsonPropertyName("pagination")]
    public required TwitchResponsePagination Pagination { get; set; }
}

public partial class TwitchVideo
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("stream_id")]
    public required string? StreamId { get; set; }

    [JsonPropertyName("user_id")]
    public required string UserId { get; set; }

    [JsonPropertyName("user_login")]
    public required string UserLogin { get; set; }

    [JsonPropertyName("user_name")]
    public required string UserName { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public required DateTimeOffset PublishedAt { get; set; }

    [JsonPropertyName("url")]
    public required Uri Url { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public required string ThumbnailUrl { get; set; }

    [JsonPropertyName("viewable")]
    public required string Viewable { get; set; }

    [JsonPropertyName("view_count")]
    public required long ViewCount { get; set; }

    [JsonPropertyName("language")]
    public required string Language { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("duration")]
    public required string Duration { get; set; }

    [JsonPropertyName("muted_segments")]
    public required List<TwitchVideoMutedSegment>? MutedSegments { get; set; }
}

public partial class TwitchVideoMutedSegment
{
    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("offset")]
    public long Offset { get; set; }
}
