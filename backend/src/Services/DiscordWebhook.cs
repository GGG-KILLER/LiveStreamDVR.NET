using System.Text.Json;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Models;
using Microsoft.Extensions.Options;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace LiveStreamDVR.Api.Services;

public interface IDiscordWebhook
{
    Task NotifyStreamStarted(TwitchStream twitchStream);
    Task NotifyStreamStopped(TwitchStream twitchStream);
}

public sealed class DiscordWebhook(IHttpClientFactory httpClientFactory, IOptionsMonitor<DiscordOptions> discordOptions) : IDiscordWebhook
{
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true, IndentSize = 2 };

    public async Task NotifyChannelUpdated(ChannelUpdate channelUpdate)
    {
        using var client = httpClientFactory.CreateClient();
        await client.PostAsJsonAsync(discordOptions.CurrentValue.WebhookUri, new DiscordWebhookMessage
        {
            Username = "LiveStreamDVR",
            Content = $"""
            **{channelUpdate.BroadcasterUserName} channel updated!**

            ```json
            {JsonSerializer.Serialize(channelUpdate, s_serializerOptions)}
            ```
            """
        });
    }

    public async Task NotifyStreamStarted(TwitchStream twitchStream)
    {
        using var client = httpClientFactory.CreateClient();
        await client.PostAsJsonAsync(discordOptions.CurrentValue.WebhookUri, new DiscordWebhookMessage
        {
            Username = "LiveStreamDVR",
            Content = $"""
            **{twitchStream.UserName} is live!**
            {twitchStream.Title}

            https://twitch.tv/{twitchStream.Login}
            """
        });
    }

    public async Task NotifyStreamStopped(TwitchStream twitchStream)
    {
        using var client = httpClientFactory.CreateClient();
        await client.PostAsJsonAsync(discordOptions.CurrentValue.WebhookUri, new DiscordWebhookMessage
        {
            Username = "LiveStreamDVR",
            Content = $"""
            **{twitchStream.UserName} has gone offline!**
            {twitchStream.Title}

            https://twitch.tv/{twitchStream.Login}
            """
        });
    }
}
