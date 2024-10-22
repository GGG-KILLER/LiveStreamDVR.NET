using LiveStreamDVR.Api.Models;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Core.SubscriptionTypes.Stream;

namespace LiveStreamDVR.Api.Services.Discord;

public interface IDiscordWebhook
{
    Task NotifyChannelUpdatedAsync(ChannelUpdate channelUpdate);
    Task NotifyStreamStartedAsync(TwitchCapture twitchStream);
    Task NotifyStreamStoppedAsync(StreamOffline streamOffline);
}
