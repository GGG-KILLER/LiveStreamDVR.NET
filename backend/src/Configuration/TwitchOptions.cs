namespace LiveStreamDVR.Api.Configuration;

public sealed class TwitchOptions
{
    public const string ConfigurationKey = "Twitch";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? WebhookSecret { get; set; }
}
