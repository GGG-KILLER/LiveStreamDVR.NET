
using System.Collections.Concurrent;
using LiveStreamDVR.Api.Models;
using LiveStreamDVR.Api.Services.Capture;
using LiveStreamDVR.Api.Services.Discord;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream;

namespace LiveStreamDVR.Api.Services.Twitch;

public sealed class TwitchEventSubService(
    ILogger<TwitchEventSubService> logger,
    IEventSubWebhooks eventSubWebhooks,
    ICaptureManager captureManager,
    IDiscordWebhook discordWebhook)
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
                await discordWebhook.NotifyChannelUpdatedAsync(@event).ConfigureAwait(false);
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

            var stream = new TwitchCapture
            {
                Id = @event.Id,
                Login = @event.BroadcasterUserLogin,
                UserName = @event.BroadcasterUserName,
                Title = streamTitle,
                StartedAt = @event.StartedAt
            };

            try
            {
                await captureManager.EnqueueCaptureAsync(stream).ConfigureAwait(false);
                await discordWebhook.NotifyStreamStartedAsync(stream).ConfigureAwait(false);
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

            try
            {
                await discordWebhook.NotifyStreamStoppedAsync(@event).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error notifying the end of a stream: {Event}", @event);
            }
        });
    }
}
