
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
        eventSubWebhooks.OnStreamOffline += OnStreamOffline;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        eventSubWebhooks.OnError -= OnError;
        eventSubWebhooks.OnChannelUpdate -= OnChannelUpdate;
        eventSubWebhooks.OnStreamOnline -= OnStreamOnline;
        eventSubWebhooks.OnStreamOffline -= OnStreamOffline;
        return Task.CompletedTask;
    }

    private void OnError(object? sender, OnErrorArgs e)
    {
        logger.LogError("EventSub error (Reason: {Reason}): {Message}", e.Reason, e.Message);
    }

    private void OnChannelUpdate(object? sender, ChannelUpdateArgs e)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                ChannelUpdate @event = e.Notification.Event;
                _channelStatus[@event.BroadcasterUserId] = @event;
                await discordWebhook.NotifyChannelUpdatedAsync(@event);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating channel status: {ChannelStatus}", e.Notification.Event);
            }
        });
    }

    private void OnStreamOnline(object? sender, StreamOnlineArgs e)
    {
        _ = Task.Run(async () =>
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
                await discordWebhook.NotifyStreamStartedAsync(stream);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error registering the start of the stream: {Stream}", stream);
            }
        });
    }

    private void OnStreamOffline(object? sender, StreamOfflineArgs e)
    {
        _ = Task.Run(async () =>
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
                await discordWebhook.NotifyStreamStoppedAsync(stream);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error notifying the end of the stream: {Stream}", stream);
            }
        });
    }
}
