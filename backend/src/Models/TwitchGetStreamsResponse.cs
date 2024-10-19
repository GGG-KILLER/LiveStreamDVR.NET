using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models
{
    public partial class TwitchGetStreamsResponse
    {
        [JsonPropertyName("data")]
        public required List<TwitchStream> Data { get; set; }

        [JsonPropertyName("pagination")]
        public required TwitchResponsePagination Pagination { get; set; }
    }

    public partial class TwitchStream
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("user_id")]
        public required string UserId { get; set; }

        [JsonPropertyName("user_login")]
        public required string UserLogin { get; set; }

        [JsonPropertyName("user_name")]
        public required string UserName { get; set; }

        [JsonPropertyName("game_id")]
        public required string GameId { get; set; }

        [JsonPropertyName("game_name")]
        public required string GameName { get; set; }

        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("tags")]
        public required List<string> Tags { get; set; }

        [JsonPropertyName("viewer_count")]
        public required long ViewerCount { get; set; }

        [JsonPropertyName("started_at")]
        public required DateTimeOffset StartedAt { get; set; }

        [JsonPropertyName("language")]
        public required string Language { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public required string ThumbnailUrl { get; set; }

        [JsonPropertyName("tag_ids")]
        public required List<string> TagIds { get; set; }

        [JsonPropertyName("is_mature")]
        public required bool IsMature { get; set; }
    }
}
