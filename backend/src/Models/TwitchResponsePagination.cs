using System.Text.Json.Serialization;

namespace LiveStreamDVR.Api.Models;

public partial class TwitchResponsePagination
{
    [JsonPropertyName("cursor")]
    public required string Cursor { get; set; }
}
