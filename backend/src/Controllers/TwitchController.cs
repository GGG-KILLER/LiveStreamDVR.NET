using System.Threading.Channels;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TwitchController(ILogger<TwitchController> logger, IOptionsMonitor<BasicOptions> basicOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async ValueTask<IActionResult> ForceCapture(
        [FromServices] Channel<TwitchStream> streams,
        [FromHeader(Name = "X-DVR-ApiKey")] string providedKey,
        [FromBody] TwitchStream stream)
    {
        if (!string.Equals(providedKey, basicOptions.CurrentValue.ApiKey, StringComparison.Ordinal))
        {
            logger.LogWarning("Invalid API key received from {IP}.", Request.HttpContext.Connection.RemoteIpAddress);
            return Unauthorized();
        }

        logger.LogWarning("Adding stream {Stream} to the queue {Hash}.", stream, $"{streams.GetHashCode():X4}");
        await streams.Writer.WriteAsync(stream);
        return Ok();
    }
}
