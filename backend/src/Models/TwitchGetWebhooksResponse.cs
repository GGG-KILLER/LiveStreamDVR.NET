using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

public partial class TwitchGetWebhooksResponse
{
    [JsonPropertyName("total")]
    public required long Total { get; set; }

    [JsonPropertyName("data")]
    public required List<TwitchWebhook> Data { get; set; }

    [JsonPropertyName("max_total_cost")]
    public required long MaxTotalCost { get; set; }

    [JsonPropertyName("total_cost")]
    public required long TotalCost { get; set; }

    [JsonPropertyName("pagination")]
    public required TwitchResponsePagination Pagination { get; set; }
}

public partial class TwitchWebhook
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("condition")]
    public required TwitchWebhookCondition Condition { get; set; }

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("transport")]
    public required TwitchWebhookTransport Transport { get; set; }

    [JsonPropertyName("cost")]
    public long Cost { get; set; }
}

public partial class TwitchWebhookCondition
{
    [JsonPropertyName("broadcaster_user_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long BroadcasterUserId { get; set; }
}

public partial class TwitchWebhookTransport
{
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("callback")]
    public required Uri Callback { get; set; }
}
