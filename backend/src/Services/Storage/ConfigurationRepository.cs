using System.Globalization;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Helpers;
using Microsoft.Extensions.Options;
using Tenray.ZoneTree;

namespace LiveStreamDVR.Api.Services.Storage;

public sealed class ConfigurationRepository(IZoneTree<string, string> database) : IConfigurationRepository
{
    public const string FfmpegExtraCommandLineKey = "config.cmds.ffmpeg-append";
    public const string StreamlinkExtraCommandLineKey = "config.cmds.streamlink-append";
    public const string DiscordWebhookUriKey = "config.discord.webhook-uri";
    public const string TwitchClientIdKey = "confing.twitch.client-id";
    public const string TwitchClientSecretKey = "config.twitch.client-secret";

    public string FfmpegExtraCommandLine
    {
        get => database.TryGet(FfmpegExtraCommandLineKey, out var value) ? value : "";
        set => database.Upsert(FfmpegExtraCommandLineKey, value);
    }

    public string StreamlinkExtraCommandLine
    {
        get => database.TryGet(StreamlinkExtraCommandLineKey, out var value) ? value : "";
        set => database.Upsert(StreamlinkExtraCommandLineKey, value);
    }

    public Uri? DiscordWebhookUri
    {
        get => database.TryGet(DiscordWebhookUriKey, out var value) ? new Uri(value) : null;
        set
        {
            if (value != null
                && (!value.IsAbsoluteUri
                    || value.Scheme != "https"
                    || value.Host is not ("discord.com" or "ptb.discord.com" or "canary.discord.com")
                    || !value.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.Ordinal)
                    || !ulong.TryParse(
                        value.AbsolutePath.AsSpan()["/api/webhooks/".Length..value.AbsolutePath.IndexOf('/', "/api/webhooks/".Length)],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out _)))
            {
                throw new ArgumentException("Provided value must be a valid discord Webhook URI", nameof(value));
            }

            database.Upsert(DiscordWebhookUriKey, value?.ToString()! /* null assertion because that's how delete works. */);
        }
    }

    public string? TwitchClientId
    {
        get => database.TryGet(TwitchClientIdKey, out var value) ? value : null;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            database.Upsert(TwitchClientIdKey, value);
        }
    }

    public string? TwitchClientSecret
    {
        get => database.TryGet(TwitchClientSecretKey, out var value) ? value : null;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            database.Upsert(TwitchClientSecretKey, value);
        }
    }

    public void InitializeFromConfiguration(
        IOptionsSnapshot<CaptureOptions> captureOptionsSnapshot,
        IOptionsSnapshot<DiscordOptions> discordOptionsSnapshot,
        IOptionsSnapshot<TwitchOptions> twitchOptionsSnapshot)
    {
        var captureOptions = captureOptionsSnapshot.Value;
        if (captureOptions.ExtraFfmpegFlags is { Length: > 0 })
        {
            database.TryAtomicAddOrUpdate(
                FfmpegExtraCommandLineKey,
                (ref string value) =>
                {
                    value = CommandLineSplitter.JoinArguments(captureOptions.ExtraFfmpegFlags);
                    return true;
                },
                (ref string value) =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = CommandLineSplitter.JoinArguments(captureOptions.ExtraFfmpegFlags);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
        }

        if (captureOptions.ExtraStreamlinkFlags is { Length: > 0 })
        {
            database.TryAtomicAddOrUpdate(
                StreamlinkExtraCommandLineKey,
                (ref string value) =>
                {
                    value = CommandLineSplitter.JoinArguments(captureOptions.ExtraStreamlinkFlags);
                    return true;
                },
                (ref string value) =>
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = CommandLineSplitter.JoinArguments(captureOptions.ExtraStreamlinkFlags);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                });
        }

        var discordOptions = discordOptionsSnapshot.Value;
        if (discordOptions.WebhookUri is not null)
        {
            database.TryAdd(DiscordWebhookUriKey, discordOptions.WebhookUri.ToString(), out _);
        }

        var twitchOptions = twitchOptionsSnapshot.Value;
        if (!string.IsNullOrWhiteSpace(twitchOptions.ClientId))
        {
            database.TryAtomicAddOrUpdate(TwitchClientIdKey, twitchOptions.ClientId, (ref string value) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = twitchOptions.ClientId;
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(twitchOptions.ClientSecret))
        {
            database.TryAtomicAddOrUpdate(TwitchClientSecretKey, twitchOptions.ClientSecret, (ref string value) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = twitchOptions.ClientSecret;
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }
    }
}
