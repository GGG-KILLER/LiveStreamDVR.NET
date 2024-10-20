using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TwitchChannelUpdateWebhookRequest), typeDiscriminator: "channel.update")]
[JsonDerivedType(typeof(TwitchStreamOnlineWebhookCondition), typeDiscriminator: "stream.online")]
[JsonDerivedType(typeof(TwitchStreamOfflineWebhookCondition), typeDiscriminator: "stream.offline")]
public abstract class TwitchWebhookRequest
{
    public required string Version { get; set; }

    public required TwitchWebhookRequestTransport Transport { get; set; }
}

public sealed class TwitchChannelUpdateWebhookRequest : TwitchWebhookRequest
{
    public required TwitchChannelUpdateWebhookCondition Condition { get; set; }
}

public sealed class TwitchChannelUpdateWebhookCondition
{
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchStreamOnlineWebhookRequest : TwitchWebhookRequest
{
    public required TwitchStreamOnlineWebhookCondition Condition { get; set; }
}

public sealed class TwitchStreamOnlineWebhookCondition
{
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchStreamOfflineWebhookRequest : TwitchWebhookRequest
{
    public required TwitchStreamOfflineWebhookCondition Condition { get; set; }
}

public sealed class TwitchStreamOfflineWebhookCondition
{
    public required string BroadcasterUserId { get; set; }
}

public sealed class TwitchWebhookRequestTransport
{
    public required string Method { get; set; }

    public required Uri Callback { get; set; }

    public required string Secret { get; set; }
}
