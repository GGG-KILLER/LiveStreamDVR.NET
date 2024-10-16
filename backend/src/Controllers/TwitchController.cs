using System.Threading.Channels;
using LiveStreamDVR.Api.Configuration;
using LiveStreamDVR.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TwitchController(IOptionsMonitor<BasicOptions> basicOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async ValueTask<IActionResult> ForceCapture(
        [FromServices] Channel<TwitchStream> streams,
        [FromHeader(Name = "X-DVR-ApiKey")] string providedKey,
        TwitchStream stream)
    {
        if (!string.Equals(providedKey, basicOptions.CurrentValue.ApiKey, StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        await streams.Writer.WriteAsync(stream);
        return Ok();
    }
}
