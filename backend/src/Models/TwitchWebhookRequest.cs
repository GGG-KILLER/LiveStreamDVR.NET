using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TwitchChannelUpdateWebhookRequest), typeDiscriminator: "channel.update")]
[JsonDerivedType(typeof(TwitchStreamOnlineWebhookCondition), typeDiscriminator: "stream.online")]
[JsonDerivedType(typeof(TwitchStreamOfflineWebhookCondition), typeDiscriminator: "stream.offline")]
public abstract class TwitchWebhookRequest
{
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("transport")]
    public required TwitchWebhookRequestTransport Transport { get; set; }
}

public sealed class TwitchChannelUpdateWebhookRequest : TwitchWebhookRequest
{
    [JsonPropertyName("condition")]
    public required TwitchChannelUpdateWebhookCondition Condition { get; set; }
}

public sealed class TwitchChannelUpdateWebhookCondition
{
    [JsonPropertyName("broadcaster_user_id")]
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchStreamOnlineWebhookRequest : TwitchWebhookRequest
{
    [JsonPropertyName("condition")]
    public required TwitchStreamOnlineWebhookCondition Condition { get; set; }
}

public sealed class TwitchStreamOnlineWebhookCondition
{
    [JsonPropertyName("broadcaster_user_id")]
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchStreamOfflineWebhookRequest : TwitchWebhookRequest
{
    [JsonPropertyName("condition")]
    public required TwitchStreamOfflineWebhookCondition Condition { get; set; }
}

public sealed class TwitchStreamOfflineWebhookCondition
{
    [JsonPropertyName("broadcaster_user_id")]
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchWebhookRequestTransport
{
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    [JsonPropertyName("callback")]
    public required Uri Callback { get; set; }

    [JsonPropertyName("secret")]
    public required string Secret { get; set; }
}
