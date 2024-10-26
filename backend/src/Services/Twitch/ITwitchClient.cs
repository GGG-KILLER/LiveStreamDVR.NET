using LiveStreamDVR.Api.Models;

namespace LiveStreamDVR.Api.Services.Twitch;

public interface ITwitchClient
{
    /// <summary>
    /// <para>Authenticates to the twich API, obtaining an access token.</para>
    /// <para>The other methods already authenticate internally so this shouldn't be needed.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token for authentication.
    /// DOES NOT unauthenticate if already authenticated. Will leave a dangling auth token.
    /// </param>
    Task AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>Revokes the token for the twitch API.</para>
    /// <para>This should only be used if your app is shutting down.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token for token revocation.
    /// </param>
    Task UnauthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether the token is POSSIBLY valid.
    /// </summary>
    /// <remarks>
    /// According to Twitch API rules, this only checks for the token validity once every hour, so the
    /// token might have been expired but this method returns <see langword="true"/>.
    /// The only full confirmation this method can give is whether a token is definitely expired or POSSIBLY valid.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token for the token validation.</param>
    /// <returns>Whether the token is POSSIBLY valid. Read remarks for more info on this.</returns>
    ValueTask<bool> VerifyTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtains a list of users by their IDs or usernames.
    /// </summary>
    /// <param name="ids">User IDs to search for.</param>
    /// <param name="names">User names to search for.</param>
    /// <param name="cancellationToken">Cancellation token for the data fetch.</param>
    /// <returns>The list of obtained users.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if no IDs and names were provided or if the amount of IDs AND usernames is more than 100.
    /// </exception>
    Task<TwitchGetUsersResponse> GetUsersAsync(IEnumerable<string>? ids = null, IEnumerable<string>? names = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtains videos from the Twitch API by their VOD ID.
    /// </summary>
    /// <remarks>
    /// Does NOT return in-progress streams, even though they have an ID of their own.
    /// </remarks>
    /// <param name="ids">The VOD IDs to obtain.</param>
    /// <param name="cancellationToken">Cancellation token for the data fetch.</param>
    /// <returns>The list of videos requested.</returns>
    Task<TwitchGetVideosResponse> GetVideosAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtains a list of streams based on the provided criteria.
    /// </summary>
    /// <param name="userIds">IDs of users to get streams of.</param>
    /// <param name="userLogins">Logins of users to get streams of.</param>
    /// <param name="gameIds">IDs of games to get streams of.</param>
    /// <param name="type">Type of streams to obtain.</param>
    /// <param name="languages">Languages of streams in to obtain.</param>
    /// <param name="first">Page size.</param>
    /// <param name="before">Pagination cursor.</param>
    /// <param name="after">Pagination cursor.</param>
    /// <param name="cancellationToken">Data fetch cancellation token.</param>
    /// <returns>Obtained streams.</returns>
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

    Task<TwitchGetWebhooksResponse> GetEventSubSubscriptionsAsync(
        string? status = null,
        string? type = null,
        string? userId = null,
        string? after = null,
        CancellationToken cancellationToken = default);

    Task<TwitchGetWebhooksResponse> CreateEventSubSubscriptionAsync(TwitchWebhookRequest webhook, CancellationToken cancellationToken = default);

    Task DeleteEventSubSubscriptionAsync(string id, CancellationToken cancellationToken = default);
}
