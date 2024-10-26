namespace LiveStreamDVR.Api.Models;

public sealed class TwitchGetWebhooksResponse
{
    public required long Total { get; set; }

    public required List<TwitchWebhook> Data { get; set; }

    public required long MaxTotalCost { get; set; }

    public required long TotalCost { get; set; }

    public TwitchResponsePagination? Pagination { get; set; }
}

public sealed class TwitchWebhook
{
    public required string Id { get; set; }

    public required string Status { get; set; }

    public required string Type { get; set; }

    public required string Version { get; set; }

    public required TwitchWebhookCondition Condition { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public required TwitchWebhookTransport Transport { get; set; }

    public long Cost { get; set; }
}

public sealed class TwitchWebhookCondition
{
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchWebhookTransport
{
    public required string Method { get; set; }

    public required Uri Callback { get; set; }
}
