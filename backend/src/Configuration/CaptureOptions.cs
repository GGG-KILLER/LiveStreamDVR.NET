namespace LiveStreamDVR.Api.Configuration;

public sealed class CaptureOptions
{
    public const string ConfigurationKey = "Capture";

    public string? OutputDirectory { get; set; }
    public string[]? ExtraStreamlinkFlags { get; set; }
    public string[]? ExtraFfmpegFlags { get; set; }
}
