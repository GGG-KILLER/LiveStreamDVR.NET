using System.ComponentModel.DataAnnotations;

namespace LiveStreamDVR.Api.Configuration;

public sealed class DiscordOptions
{
    public const string ConfigurationKey = "Discord";

    [Required]
    public required Uri WebhookUri { get; set; }
}
