using System.ComponentModel.DataAnnotations;

namespace LiveStreamDVR.Api.Configuration;

public sealed class BinariesOptions
{
    public const string ConfigurationKey = "Binaries";

    [Required]
    public required string StreamLinkPath { get; set; }
    [Required]
    public required string FfmpegPath { get; set; }
    public string? MediaInfoPath { get; set; }
    public string? TwitchDownloaderCliPath { get; set; }
}
