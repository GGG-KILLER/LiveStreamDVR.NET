namespace LiveStreamDVR.Api.Configuration;

public sealed class DiscordOptions
{
    public const string ConfigurationKey = "Discord";

    public Uri? WebhookUri { get; set; }
}
