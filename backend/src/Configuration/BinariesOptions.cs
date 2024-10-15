namespace LiveStreamDVR.Api.Configuration;

public sealed class BinariesOptions
{
    public const string ConfigurationKey = "Binaries";

    public string? StreamLinkPath { get; set; }
    public string? FfmpegPath { get; set; }
    public string? MediaInfoPath { get; set; }
    public string? TwitchDownloaderCliPath { get; set; }
}
