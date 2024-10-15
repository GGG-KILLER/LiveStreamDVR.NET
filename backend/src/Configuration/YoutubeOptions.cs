namespace LiveStreamDVR.Api.Configuration;

public sealed class YoutubeOptions
{
    public const string ConfigurationKey = "Youtube";

    public string? ClientId { get; set; }
    public string? Secret { get; set; }
}
