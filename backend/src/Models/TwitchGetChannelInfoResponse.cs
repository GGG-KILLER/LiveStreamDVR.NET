using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

public partial class TwitchGetChannelInfoResponse
{
    [JsonPropertyName("data")]
    public required List<TwitchChannelInfo> Data { get; set; }
}

public partial class TwitchChannelInfo
{
    [JsonPropertyName("broadcaster_id")]
    public required string BroadcasterId { get; set; }

    [JsonPropertyName("broadcaster_login")]
    public required string BroadcasterLogin { get; set; }

    [JsonPropertyName("broadcaster_name")]
    public required string BroadcasterName { get; set; }

    [JsonPropertyName("broadcaster_language")]
    public required string BroadcasterLanguage { get; set; }

    [JsonPropertyName("game_id")]
    public required string GameId { get; set; }

    [JsonPropertyName("game_name")]
    public required string GameName { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("delay")]
    public required long Delay { get; set; }

    [JsonPropertyName("tags")]
    public required List<string> Tags { get; set; }

    [JsonPropertyName("content_classification_labels")]
    public required List<string> ContentClassificationLabels { get; set; }

    [JsonPropertyName("is_branded_content")]
    public required bool IsBrandedContent { get; set; }
}
