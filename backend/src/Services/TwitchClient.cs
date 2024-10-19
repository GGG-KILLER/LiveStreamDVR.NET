
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;

namespace LiveStreamDVR.Api.Services;

public sealed class TwitchClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<TwitchOptions> twitchOptionsMonitor)
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

    public async Task<TwitchGetUsersResponse> GetUsersAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"users?user_login={string.Join("&user_login=", names)}");
        using var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TwitchGetUsersResponse>(cancellationToken).ConfigureAwait(false))!;
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

            using var _2 = request;
            request.Headers.Add("Authorization", $"Bearer {_accessToken!.AccessToken}");

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
}
