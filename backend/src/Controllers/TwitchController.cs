using LiveStreamDVR.Api.Models;
using LiveStreamDVR.Api.Services.Capture;
using LiveStreamDVR.Api.Services.Twitch;
using Microsoft.AspNetCore.Mvc;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TwitchController(ITwitchClient twitchClient, ILogger<TwitchController> logger) : ControllerBase
{
    [HttpPost("[action]")]
    [ProducesResponseType<TwitchCapture>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ForceCaptureAsync(
        ICaptureManager captureManager,
        Uri uri,
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
    public async Task<IActionResult> GetStreamsAsync(Uri uri, CancellationToken cancellationToken = default)
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
    public async Task<IActionResult> GetVideoAsync(Uri uri, CancellationToken cancellationToken = default)
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
}
