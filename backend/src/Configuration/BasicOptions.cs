using System.ComponentModel.DataAnnotations;

namespace LiveStreamDVR.Api.Configuration;

public sealed class BasicOptions
{
    public const string ConfigurationKey = "Basic";

    [Required]
    public required Uri PublicUri { get; set; }
}
