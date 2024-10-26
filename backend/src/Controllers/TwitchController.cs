using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Exceptions;
using LiveStreamDVR.Api.Models;
using LiveStreamDVR.Api.Services.Capture;
using LiveStreamDVR.Api.Services.Storage;
using LiveStreamDVR.Api.Services.Twitch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TwitchController(
    IOptionsSnapshot<BasicOptions> basicOptionsSnapshot,
    IOptionsSnapshot<TwitchOptions> twitchOptionsSnapshot,
    ITwitchClient twitchClient,
    ITwitchRepository twitchRepository,
    ILogger<TwitchController> logger) : ControllerBase
{
    [HttpPost("[action]")]
    [ProducesResponseType<TwitchCapture>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ForceCaptureAsync(
        [FromServices] ICaptureManager captureManager,
        [Required, FromQuery] Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (!uri.IsAbsoluteUri
            || uri.Scheme != "https"
            || (uri.Host != "twitch.tv" && !uri.Host.EndsWith(".twitch.tv", StringComparison.Ordinal))
            || uri.PathAndQuery.StartsWith("/videos/", StringComparison.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URI provided.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "URL must be in the format https://www.twitch.tv/user-login",
            });
        }

        var name = uri.AbsolutePath["/".Length..];
        if (name.Contains('/'))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URI provided.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "URL must be in the format https://www.twitch.tv/user-login",
            });
        }

        var response = await twitchClient.GetStreamsAsync(userLogins: [name], cancellationToken: cancellationToken);
        if (response.Data.Count < 1)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No streams found.",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"No streams were found, cannot capture anything."
            });
        }
        else if (response.Data.Count > 1)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "More than one stream found.",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"{response.Data.Count} streams were found, cannot tell which one should be captured.",
                Extensions = new Dictionary<string, object?>
                {
                    { "streams", response.Data }
                }
            });
        }

        TwitchStream twitchStream = response.Data[0];
        logger.LogInformation("ForceCapture: Found stream {Stream}", twitchStream);

        var capture = new TwitchCapture
        {
            Id = twitchStream.Id,
            Login = twitchStream.UserLogin,
            UserName = twitchStream.UserName,
            Title = twitchStream.Title,
            StartedAt = twitchStream.StartedAt
        };
        logger.LogInformation("ForceCapture: Adding {Capture} to the queue.", capture);
        await captureManager.EnqueueCaptureAsync(capture, cancellationToken).ConfigureAwait(false);

        return Ok(capture);
    }

    [HttpGet("[action]")]
    [ProducesResponseType<TwitchGetStreamsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStreamsAsync(
        [Required] Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (!uri.IsAbsoluteUri
            || uri.Scheme != "https"
            || (uri.Host != "twitch.tv" && !uri.Host.EndsWith(".twitch.tv", StringComparison.Ordinal))
            || uri.PathAndQuery.StartsWith("/videos/", StringComparison.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URI provided.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "URL must be in the format https://www.twitch.tv/user-login",
            });
        }

        var name = uri.AbsolutePath["/".Length..];
        if (name.Contains('/'))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URI provided.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "URL must be in the format https://www.twitch.tv/user-login",
            });
        }

        var response = await twitchClient.GetStreamsAsync(userLogins: [name], cancellationToken: cancellationToken);
        return Ok(response);
    }

    [HttpGet("[action]")]
    [ProducesResponseType<TwitchGetVideosResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetVideoAsync(
        [Required] Uri uri,
        CancellationToken cancellationToken = default)
    {
        if (!uri.IsAbsoluteUri
            || uri.Scheme != "https"
            || (uri.Host != "twitch.tv" && !uri.Host.EndsWith(".twitch.tv", StringComparison.Ordinal))
            || !uri.PathAndQuery.StartsWith("/videos/", StringComparison.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URI provided.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "URL must be in the format https://www.twitch.tv/videos/00000000",
            });
        }

        var id = uri.AbsolutePath["/videos/".Length..];
        if (id.Contains('/'))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid URI provided.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "URL must be in the format https://www.twitch.tv/videos/00000000",
            });
        }

        var response = await twitchClient.GetVideosAsync([id], cancellationToken);
        return Ok(response);
    }

    [HttpGet("Subscriptions")]
    [ProducesResponseType<TwitchUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var subscriptions = await twitchClient.GetEventSubSubscriptionsAsync(cancellationToken: cancellationToken);
        while (subscriptions.Pagination?.Cursor != null)
        {
            var temp = subscriptions;
            subscriptions = await twitchClient.GetEventSubSubscriptionsAsync(subscriptions.Pagination.Cursor, cancellationToken);
            subscriptions.Data.InsertRange(0, temp.Data);
        }

        return Ok(subscriptions);
    }

    [HttpGet("Streamer/{nameOrId:required:regex(^([[a-zA-Z0-9_]]{{4,25}}|\\d+)$)}")]
    [ProducesResponseType<TwitchUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStreamerAsync(
        [FromRoute] string nameOrId,
        CancellationToken cancellationToken = default)
    {
        TwitchGetUsersResponse response;
        if (long.TryParse(nameOrId, out _))
        {
            response = await twitchClient.GetUsersAsync(ids: [nameOrId], cancellationToken: cancellationToken);
        }
        else
        {
            response = await twitchClient.GetUsersAsync(names: [nameOrId], cancellationToken: cancellationToken);
        }

        if (response.Data.Count == 0)
            return NotFound();

        var user = response.Data[0];
        twitchRepository.SetStreamerId(user.Login, user.Id);
        return Ok(user);
    }

    [HttpPost("Streamer/{nameOrId:required:regex(^([[a-zA-Z0-9_]]{{4,25}}|\\d+)$)}/subscribe")]
    [ProducesResponseType<TwitchUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubscribeToStreamerAsync(
        [FromRoute] string nameOrId,
        CancellationToken cancellationToken = default)
    {
        var basicOptions = basicOptionsSnapshot.Value;
        var twitchOptions = twitchOptionsSnapshot.Value;

        string id;
        if (long.TryParse(nameOrId, out _))
        {
            id = nameOrId;
        }
        else if (twitchRepository.GetStreamerId(nameOrId) is { } tempId)
        {
            id = tempId;
        }
        else
        {
            var response = await twitchClient.GetUsersAsync(names: [nameOrId], cancellationToken: cancellationToken);
            if (response.Data.Count == 0)
                return NotFound();

            var user = response.Data[0];
            twitchRepository.SetStreamerId(user.Login, user.Id);
            id = user.Id;
        }

        var webhookUri = new Uri(basicOptions.PublicUri, "hook/twitch");
        var subscriptions = new List<TwitchGetWebhooksResponse>(3);
        try
        {
            subscriptions.Add(await twitchClient.CreateEventSubSubscriptionAsync(
                new TwitchChannelUpdateWebhookRequest(id, webhookUri, twitchOptions.WebhookSecret),
                CancellationToken.None));

            cancellationToken.ThrowIfCancellationRequested();

            subscriptions.Add(await twitchClient.CreateEventSubSubscriptionAsync(
                new TwitchStreamOnlineWebhookRequest(id, webhookUri, twitchOptions.WebhookSecret),
                CancellationToken.None));

            cancellationToken.ThrowIfCancellationRequested();

            subscriptions.Add(await twitchClient.CreateEventSubSubscriptionAsync(
                new TwitchStreamOfflineWebhookRequest(id, webhookUri, twitchOptions.WebhookSecret),
                CancellationToken.None));
        }
        catch (TwitchRequestException ex)
        {
            foreach (var subscription in subscriptions.SelectMany(x => x.Data))
            {
                try
                {
                    await twitchClient.DeleteEventSubSubscriptionAsync(subscription.Id, CancellationToken.None);
                }
                catch (Exception ex2)
                {
                    logger.LogError(ex2, "Error unsubscribing from already subscribed events: {Events}", subscriptions);
                }
            }

            logger.LogError(ex, "Error subscribing to event stream.online for streamer {Streamer}", nameOrId);

            // Return the same response as Twitch.
            return new ContentResult
            {
                StatusCode = (int)ex.ResponseStatusCode,
                ContentType = MediaTypeNames.Application.Json,
                Content = ex.ResponseContent
            };
        }

        return Ok(new TwitchGetWebhooksResponse
        {
            Total = subscriptions.Sum(x => x.Total),
            Data = subscriptions.SelectMany(x => x.Data).ToList(),
            TotalCost = subscriptions.Sum(x => x.TotalCost),
            MaxTotalCost = subscriptions.Max(x => x.MaxTotalCost),
            Pagination = new TwitchResponsePagination(),
        });
    }

    [HttpPost("Streamer/{nameOrId:required:regex(^([[a-zA-Z0-9_]]{{4,25}}|\\d+)$)}/unsubscribe")]
    [ProducesResponseType<TwitchUser>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnsubscribeToStreamerAsync(
        [FromRoute] string nameOrId,
        CancellationToken cancellationToken = default)
    {
        var basicOptions = basicOptionsSnapshot.Value;
        var twitchOptions = twitchOptionsSnapshot.Value;

        string id;
        if (long.TryParse(nameOrId, out _))
        {
            id = nameOrId;
        }
        else if (twitchRepository.GetStreamerId(nameOrId) is { } tempId)
        {
            id = tempId;
        }
        else
        {
            var response = await twitchClient.GetUsersAsync(names: [nameOrId], cancellationToken: cancellationToken);
            if (response.Data.Count == 0)
                return NotFound();

            var user = response.Data[0];
            twitchRepository.SetStreamerId(user.Login, user.Id);
            id = user.Id;
        }

        var subscriptions = await twitchClient.GetEventSubSubscriptionsAsync(cancellationToken: cancellationToken);
        foreach (var subscription in subscriptions.Data)
        {
            try
            {
                await twitchClient.DeleteEventSubSubscriptionAsync(subscription.Id, CancellationToken.None);
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, "Error unsubscribing from already subscribed events: {Events}", subscriptions);
            }
        }

        return Ok();
    }
}
