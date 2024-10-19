using System.Threading.Channels;
using LiveStreamDVR.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TwitchController(ILogger<TwitchController> logger) : ControllerBase
{
    [HttpPost("[action]")]
    public async ValueTask<IActionResult> ForceCaptureAsync(
        [FromServices] Channel<TwitchCapture> streams,
        [FromBody] TwitchCapture stream)
    {
        logger.LogWarning("ForceCapture: Adding stream {Stream} to the queue.", stream);
        await streams.Writer.WriteAsync(stream).ConfigureAwait(false);
        return Ok();
    }
}
