using System.Text.Json;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Models;
using Microsoft.Extensions.Options;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Core.SubscriptionTypes.Stream;

namespace LiveStreamDVR.Api.Services;

public interface IDiscordWebhook
{
    Task NotifyChannelUpdatedAsync(ChannelUpdate channelUpdate);
    Task NotifyStreamStartedAsync(TwitchCapture twitchStream);
    Task NotifyStreamStoppedAsync(StreamOffline streamOffline);
}

public sealed class DiscordWebhook(IHttpClientFactory httpClientFactory, IOptionsMonitor<DiscordOptions> discordOptions) : IDiscordWebhook
{
    private static readonly JsonSerializerOptions s_serializerOptions = new() { WriteIndented = true, IndentSize = 2 };

    public async Task NotifyChannelUpdatedAsync(ChannelUpdate channelUpdate)
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
        }).ConfigureAwait(false);
    }

    public async Task NotifyStreamStartedAsync(TwitchCapture twitchStream)
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
        }).ConfigureAwait(false);
    }

    public async Task NotifyStreamStoppedAsync(StreamOffline streamOffline)
    {
        using var client = httpClientFactory.CreateClient();
        await client.PostAsJsonAsync(discordOptions.CurrentValue.WebhookUri, new DiscordWebhookMessage
        {
            Username = "LiveStreamDVR",
            Content = $"""
            **{streamOffline.BroadcasterUserName} has gone offline!**

            https://twitch.tv/{streamOffline.BroadcasterUserLogin}
            """
        }).ConfigureAwait(false);
    }
}
