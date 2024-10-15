namespace LiveStreamDVR.Api.Configuration;

public sealed class DiscordOptions
{
    public const string ConfigurationKey = "Discord";

    public string? WebhookUri { get; set; }
}
