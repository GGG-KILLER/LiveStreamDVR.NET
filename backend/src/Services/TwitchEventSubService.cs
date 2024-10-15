
using System.Collections.Concurrent;
using System.Threading.Channels;
using LiveStreamDVR.Api.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream;

namespace LiveStreamDVR.Api.Services;

public sealed class TwitchEventSubService(
    ILogger<TwitchEventSubService> logger,
    IEventSubWebhooks eventSubWebhooks,
    IDiscordWebhook discordWebhook,
    Channel<TwitchStream> streams)
    : IHostedService
{
    private readonly ConcurrentDictionary<string, ChannelUpdate> _channelStatus = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        eventSubWebhooks.OnError += OnError;
        eventSubWebhooks.OnChannelUpdate += OnChannelUpdate;
        eventSubWebhooks.OnStreamOnline += OnStreamOnline;
        eventSubWebhooks.OnStreamOffline += OnStreamOfflineAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        eventSubWebhooks.OnError -= OnError;
        eventSubWebhooks.OnStreamOnline -= OnStreamOnline;
        eventSubWebhooks.OnStreamOffline -= OnStreamOfflineAsync;
        return Task.CompletedTask;
    }

    private void OnError(object? sender, OnErrorArgs e)
    {
        logger.LogError("EventSub error (Reason: {Reason}): {Message}", e.Reason, e.Message);
    }

    private void OnChannelUpdate(object? sender, ChannelUpdateArgs e)
    {
        _channelStatus[e.Notification.Event.BroadcasterUserId] = e.Notification.Event;
    }

    private async void OnStreamOnline(object? sender, StreamOnlineArgs e)
    {
        var @event = e.Notification.Event;
        var streamTitle = "Unknown title";
        if (_channelStatus.TryGetValue(@event.BroadcasterUserId, out var update))
        {
            streamTitle = update.Title;
        }

        var stream = new TwitchStream
        {
            Id = @event.Id,
            Login = @event.BroadcasterUserLogin,
            UserName = @event.BroadcasterUserName,
            Title = streamTitle,
            StartedAt = @event.StartedAt
        };

        try
        {
            await streams.Writer.WriteAsync(stream);
            await discordWebhook.NotifyStreamStarted(stream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering the start of the stream: {Stream}", stream);
        }
    }

    private async void OnStreamOfflineAsync(object? sender, StreamOfflineArgs e)
    {
        var @event = e.Notification.Event;
        var streamTitle = "Unknown title";
        if (_channelStatus.TryGetValue(@event.BroadcasterUserId, out var update))
        {
            streamTitle = update.Title;
        }
        var stream = new TwitchStream
        {
            Login = @event.BroadcasterUserLogin,
            UserName = @event.BroadcasterUserName,
            Title = streamTitle,
        };

        try
        {
            await discordWebhook.NotifyStreamStopped(stream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error notifying the end of the stream: {Stream}", stream);
        }
    }
}
