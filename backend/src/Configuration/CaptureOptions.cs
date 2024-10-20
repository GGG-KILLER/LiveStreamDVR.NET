using System.ComponentModel.DataAnnotations;

namespace LiveStreamDVR.Api.Configuration;

public sealed class CaptureOptions
{
    public const string ConfigurationKey = "Capture";

    [Required]
    public required string OutputDirectory { get; set; }
    public string[]? ExtraStreamlinkFlags { get; set; }
    public string[]? ExtraFfmpegFlags { get; set; }
}
