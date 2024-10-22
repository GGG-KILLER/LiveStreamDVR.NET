using System.ComponentModel.DataAnnotations;

namespace LiveStreamDVR.Api.Configuration;

public sealed class TwitchOptions
{
    public const string ConfigurationKey = "Twitch";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    [Required, MinLength(10), MaxLength(100)]
    public required string WebhookSecret { get; set; }
}
