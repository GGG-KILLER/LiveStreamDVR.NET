using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models
{
    public partial class TwitchGetUsersResponse
    {
        [JsonPropertyName("data")]
        public required List<TwitchUser> Data { get; set; }
    }

    public partial class TwitchUser
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("login")]
        public required string Login { get; set; }

        [JsonPropertyName("display_name")]
        public required string DisplayName { get; set; }

        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("broadcaster_type")]
        public required string BroadcasterType { get; set; }

        [JsonPropertyName("description")]
        public required string Description { get; set; }

        [JsonPropertyName("profile_image_url")]
        public required Uri ProfileImageUrl { get; set; }

        [JsonPropertyName("offline_image_url")]
        public required Uri OfflineImageUrl { get; set; }

        [JsonPropertyName("view_count")]
        public required long ViewCount { get; set; }

        [JsonPropertyName("email")]
        public required string Email { get; set; }

        [JsonPropertyName("created_at")]
        public required DateTimeOffset CreatedAt { get; set; }
    }
}
