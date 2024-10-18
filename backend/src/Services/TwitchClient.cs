
using System.Text.Json.Serialization;
using LiveStreamDVR.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;

namespace LiveStreamDVR.Api.Services;

public sealed class TwitchClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<TwitchOptions> twitchOptionsMonitor)
{
    private readonly AsyncReaderWriterLock _tokenLock = new(null);
    private TwitchAcessToken? _acessToken = null;

    public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        if (_acessToken?.ExpirationDate > DateTime.Now)
        {
            return;
        }

        await using var _1 = await _tokenLock.WriteLockAsync(cancellationToken);

        // Could've obtained the token from another lock.
        if (_acessToken?.ExpirationDate > DateTime.Now)
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
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        _acessToken = await response.Content.ReadFromJsonAsync<TwitchAcessToken>(cancellationToken: cancellationToken);
        _acessToken!.ClientId = options.ClientId;
    }

    public async Task UnauthenticateAsync(CancellationToken cancellationToken = default)
    {
        var token = _acessToken;
        _acessToken = null;
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
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        // We do not care about the response, the token is either revoked or invalid by the end of this.
    }

    public async ValueTask<bool> VerifyTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_acessToken is null || _acessToken.ExpirationDate <= DateTime.Now)
        {
            return false;
        }

        await using var _1 = await _tokenLock.UpgradeableReadLockAsync(cancellationToken);

        // Could've obtained the token from another lock.
        if (_acessToken is null || _acessToken.ExpirationDate <= DateTime.Now)
        {
            return false;
        }

        var options = twitchOptionsMonitor.CurrentValue;
        using var request = new HttpRequestMessage(HttpMethod.Post, "validate")
        {
            Headers = {
                { "Authorization", $"OAuth {_acessToken.AccessToken}" }
            }
        };

        using var httpClient = httpClientFactory.CreateClient("TwitchOauth");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // NOTE: We use no cancellation token here because it's important that we null out the token.
            await using var _2 = await _tokenLock.WriteLockAsync(CancellationToken.None);
            _acessToken = null;
            return false;
        }

        return true;
    }

    private class TwitchAcessToken
    {
        public string ClientId { get; set; } = null!;

        [JsonPropertyName("access_token")]
        public required string AccessToken { get; set; }

        [JsonIgnore]
        public DateTime ExpirationDate { get; set; }

        [JsonPropertyName("expires_in")]
        public required long ExpiresIn
        {
            get => (ExpirationDate - DateTime.Now).Seconds;
            set => ExpirationDate = DateTime.Now.AddSeconds(value);
        }

        [JsonPropertyName("token_type")]
        public required string TokenType { get; set; }
    }
}
