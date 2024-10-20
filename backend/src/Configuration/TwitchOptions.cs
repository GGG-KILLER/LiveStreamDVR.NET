using System.ComponentModel.DataAnnotations;

namespace LiveStreamDVR.Api.Configuration;

public sealed class TwitchOptions
{
    public const string ConfigurationKey = "Twitch";

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }

    [Required, MinLength(10), MaxLength(100)]
    public required string WebhookSecret { get; set; }
}
