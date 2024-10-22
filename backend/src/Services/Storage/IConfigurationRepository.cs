using LiveStreamDVR.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Services.Storage;

public interface IConfigurationRepository
{
    /// <summary>
    /// The extra arguments to add to the ffmpeg command.
    /// </summary>
    string FfmpegExtraCommandLine { get; set; }

    /// <summary>
    /// The extra arguments to add to the streamlink command.
    /// </summary>
    string StreamlinkExtraCommandLine { get; set; }

    /// <summary>
    /// The URL of the discord webhook to use when notifying of updates.
    /// </summary>
    Uri? DiscordWebhookUri { get; set; }

    /// <summary>
    /// The Twitch client ID to use when communicating with the Twitch API.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value passed to setter is null or whitespace.</exception>
    string? TwitchClientId { get; set; }

    /// <summary>
    /// The Twitch client secret to use when communicating with the Twitch API.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when value passed to setter is null or whitespace.</exception>
    string? TwitchClientSecret { get; set; }

    /// <summary>
    /// Migrates the old settings from the immutable config method.
    /// </summary>
    void InitializeFromConfiguration(
        IOptionsSnapshot<CaptureOptions> captureOptionsSnapshot,
        IOptionsSnapshot<DiscordOptions> discordOptionsSnapshot,
        IOptionsSnapshot<TwitchOptions> twitchOptionsSnapshot);
}
