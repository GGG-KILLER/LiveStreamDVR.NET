namespace LiveStreamDVR.Api.Configuration;

public sealed class TwitchOptions
{
    public const string ConfigurationKey = "Twitch";

    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public required string WebhookSecret { get; set; }
}
