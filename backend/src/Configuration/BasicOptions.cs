namespace LiveStreamDVR.Api.Configuration;

public sealed class BasicOptions
{
    public const string ConfigurationKey = "Basic";

    public string? PublicUri { get; set; }
    public string? PathPrefix { get; set; }
}
