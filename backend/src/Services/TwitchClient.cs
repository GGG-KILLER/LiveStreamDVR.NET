
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Exceptions;
using LiveStreamDVR.Api.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;

namespace LiveStreamDVR.Api.Services;

public sealed class TwitchClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<TwitchOptions> twitchOptionsMonitor) : ITwitchClient, IDisposable
{
    private readonly AsyncReaderWriterLock _tokenLock = new(null);
    private TwitchAcessToken? _accessToken = null;
    private DateTime _lastTokenVerification = DateTime.MinValue;

    [MemberNotNull(nameof(_accessToken))]
    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable CS8774
        if (_accessToken?.ExpirationDate > DateTime.Now)
        {
            return;
        }

        await using var _1 = await _tokenLock.WriteLockAsync(cancellationToken);

        // Could've obtained the token from another lock.
        if (_accessToken?.ExpirationDate > DateTime.Now)
        {
            return;
        }

        var options = twitchOptionsMonitor.CurrentValue;
        using var request = new HttpRequestMessage(HttpMethod.Post, "token")
        {
            Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
            ])
        };

        using var httpClient = httpClientFactory.CreateClient("TwitchOauth");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                             .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        _accessToken = (await response.Content.ReadFromJsonAsync<TwitchAcessToken>(cancellationToken: cancellationToken)
                                              .ConfigureAwait(false))!;
        _accessToken.ClientId = options.ClientId;
        _lastTokenVerification = DateTime.Now; // We just got the token, let's only validate on the next hour.
#pragma warning restore CS8774
    }

    public async Task UnauthenticateAsync(CancellationToken cancellationToken = default)
    {
        var token = _accessToken;
        _accessToken = null;
        if (token is null || token.ExpirationDate <= DateTime.Now)
        {
            return;
        }

        await using var _1 = await _tokenLock.WriteLockAsync(cancellationToken);

        // Lock could expire after the wait time, then we won't need to revoke it.
        if (token.ExpirationDate <= DateTime.Now)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "revoke")
        {
            Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", token.ClientId),
                new KeyValuePair<string, string>("token", token.AccessToken),
            ])
        };

        using var httpClient = httpClientFactory.CreateClient("TwitchOauth");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                             .ConfigureAwait(false);
        // We do not care about the response, the token is either revoked or invalid by the end of this.
    }

    [MemberNotNullWhen(true, nameof(_accessToken))]
    public async ValueTask<bool> VerifyTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_accessToken is null || _accessToken.ExpirationDate <= DateTime.Now)
        {
            return false;
        }

        // Only actually call their API to check every hour.
        if ((DateTime.Now - _lastTokenVerification).TotalMinutes <= 60)
        {
            return true;
        }

        await using var _1 = await _tokenLock.UpgradeableReadLockAsync(cancellationToken);

        // Could've obtained the token from another lock.
        if (_accessToken is null || _accessToken.ExpirationDate <= DateTime.Now)
        {
            return false;
        }

        // Only actually call their API to check every hour.
        if ((DateTime.Now - _lastTokenVerification).TotalMinutes <= 60)
        {
            return true;
        }

        var options = twitchOptionsMonitor.CurrentValue;
        using var request = new HttpRequestMessage(HttpMethod.Post, "validate")
        {
            Headers = {
                { "Authorization", $"OAuth {_accessToken.AccessToken}" }
            }
        };

        using var httpClient = httpClientFactory.CreateClient("TwitchOauth");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                                             .ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // NOTE: We use no cancellation token here because it's important that we null out the token.
            await using var _2 = await _tokenLock.WriteLockAsync(CancellationToken.None);
            _accessToken = null;
            return false;
        }

        _lastTokenVerification = DateTime.Now;
        return true;
    }

    public async Task<TwitchGetUsersResponse> GetUsersAsync(
        IEnumerable<string>? ids = null,
        IEnumerable<string>? names = null,
        CancellationToken cancellationToken = default)
    {
        var uri = QueryHelpers.AddQueryString(
            "users",
            [
                ..(ids?.Select(id => KeyValuePair.Create("id", id)) ?? []),
                ..(names?.Select(name => KeyValuePair.Create("login", name)) ?? []),
            ]);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwitchGetUsersResponse>(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<TwitchGetVideosResponse> GetVideosAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        string uri = QueryHelpers.AddQueryString("videos", ids.Select(id => KeyValuePair.Create("id", id))!);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwitchGetVideosResponse>(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<TwitchGetStreamsResponse> GetStreamsAsync(
        IEnumerable<string>? userIds = null,
        IEnumerable<string>? userLogins = null,
        IEnumerable<string>? gameIds = null,
        string? type = null,
        IEnumerable<string>? languages = null,
        int? first = null,
        string? before = null,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var uri = QueryHelpers.AddQueryString(
            "streams",
            [
                ..(userIds?.Select(id => KeyValuePair.Create("user_id", id)) ?? []),
                ..(userLogins?.Select(login => KeyValuePair.Create("user_login", login)) ?? []),
                ..(gameIds?.Select(id => KeyValuePair.Create("game_id", id)) ?? []),
                KeyValuePair.Create("type", type),
                ..(languages?.Select(language => KeyValuePair.Create("language", language)) ?? []),
                KeyValuePair.Create("first", first?.ToString()),
                KeyValuePair.Create("before", before),
                KeyValuePair.Create("after", after),
            ]);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwitchGetStreamsResponse>(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<TwitchGetChannelInfoResponse> GetChannelsInfoAsync(
        IEnumerable<string> ids,
        CancellationToken cancellationToken = default)
    {
        var uri = QueryHelpers.AddQueryString(
            "channels",
            ids.Select(id => KeyValuePair.Create("broadcaster_id", id))!);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwitchGetChannelInfoResponse>(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<TwitchGetWebhooksResponse> GetEventSubSubscriptionsAsync(
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        string uri = QueryHelpers.AddQueryString("eventsub/subscriptions", [KeyValuePair.Create("after", after)]);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwitchGetWebhooksResponse>(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<TwitchGetWebhooksResponse> CreateEventSubSubscriptionAsync(
        TwitchWebhookRequest webhook,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "eventsub/subscriptions")
        {
            Content = JsonContent.Create(webhook)
        };
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new TwitchRequestException(
                "Failed to create EventSub subscription.",
                requestUri: response.RequestMessage!.RequestUri!,
                requestContent: await response.RequestMessage!.Content!.ReadAsStringAsync(CancellationToken.None),
                responseStatusCode: response.StatusCode,
                responseContent: await response.Content.ReadAsStringAsync(CancellationToken.None));
        }
        return (await response.Content.ReadFromJsonAsync<TwitchGetWebhooksResponse>(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task DeleteEventSubSubscriptionAsync(string id, CancellationToken cancellationToken = default)
    {
        var uri = QueryHelpers.AddQueryString("eventsub/subscriptions", "id", id);
        using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new TwitchRequestException(
                "Failed to delete EventSub subscription.",
                requestUri: response.RequestMessage!.RequestUri!,
                requestContent: await response.RequestMessage!.Content!.ReadAsStringAsync(CancellationToken.None),
                responseStatusCode: response.StatusCode,
                responseContent: await response.Content.ReadAsStringAsync(CancellationToken.None));
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        await using var _1 = await _tokenLock.UpgradeableReadLockAsync(AsyncReaderWriterLock.LockFlags.StickyWrite, cancellationToken);

        var retry = false;
        do
        {
            // Authenticate if our token expired.
            if (!await VerifyTokenAsync(cancellationToken).ConfigureAwait(false))
            {
                await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
            }

            var token = _accessToken!;
            using var _2 = request;
            request.Headers.Add("Authorization", $"Bearer {token.AccessToken}");
            request.Headers.Add("Client-Id", token.ClientId);

            using var httpClient = httpClientFactory.CreateClient("TwitchHelix");
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (!retry)
                {
                    // Retry if this is our first time, as we only check for invalid tokens every
                    // hour and it could've expired midway for some other reason.
                    retry = true;
                    // NOTE: We use no cancellation token here because it's important that we null out the token.
                    await using (var _3 = await _tokenLock.WriteLockAsync(CancellationToken.None))
                        _accessToken = null;
                    await AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                    continue;
                }
                else
                {
                    throw new InvalidOperationException("Unable to get valid token to send request.");
                }
            }

            return response;
        }
        while (retry);

        throw new Exception("Unreacheable code point reached.");
    }

    private class TwitchAcessToken
    {
        public string ClientId { get; set; } = null!;

        [JsonPropertyName("access_token")]
        public required string AccessToken { get; init; }

        [JsonIgnore]
        public DateTime ExpirationDate { get; init; }

        [JsonPropertyName("expires_in")]
        public required long ExpiresIn
        {
            get => (ExpirationDate - DateTime.Now).Seconds;
            init => ExpirationDate = DateTime.Now.AddSeconds(value);
        }

        [JsonPropertyName("token_type")]
        public required string TokenType { get; init; }
    }

    public void Dispose()
    {
        _tokenLock.Dispose();
    }
}
