namespace LiveStreamDVR.Api.Models;

public sealed record TwitchCapture
{
    /// <summary>
    /// The id of the stream.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The broadcaster's user display name.
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// The broadcaster's user login.
    /// </summary>
    public required string Login { get; init; }

    /// <summary>
    /// The channel’s stream title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The timestamp at which the stream went online at.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }
}
