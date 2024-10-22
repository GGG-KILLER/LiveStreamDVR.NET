using LiveStreamDVR.Api.Models;

namespace LiveStreamDVR.Api.Services.Twitch;

public interface ITwitchClient
{
    Task AuthenticateAsync(CancellationToken cancellationToken = default);
    Task UnauthenticateAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> VerifyTokenAsync(CancellationToken cancellationToken = default);
    Task<TwitchGetUsersResponse> GetUsersAsync(IEnumerable<string>? ids = null, IEnumerable<string>? names = null, CancellationToken cancellationToken = default);
    Task<TwitchGetVideosResponse> GetVideosAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<TwitchGetStreamsResponse> GetStreamsAsync(
        IEnumerable<string>? userIds = null,
        IEnumerable<string>? userLogins = null,
        IEnumerable<string>? gameIds = null,
        string? type = null,
        IEnumerable<string>? languages = null,
        int? first = null,
        string? before = null,
        string? after = null,
        CancellationToken cancellationToken = default);
    Task<TwitchGetChannelInfoResponse> GetChannelsInfoAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<TwitchGetWebhooksResponse> GetEventSubSubscriptionsAsync(string? after = null, CancellationToken cancellationToken = default);
    Task<TwitchGetWebhooksResponse> CreateEventSubSubscriptionAsync(TwitchWebhookRequest webhook, CancellationToken cancellationToken = default);
    Task DeleteEventSubSubscriptionAsync(string id, CancellationToken cancellationToken = default);
}
