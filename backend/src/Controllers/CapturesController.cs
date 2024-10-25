using LiveStreamDVR.Api.Models;
using LiveStreamDVR.Api.Services.Capture;
using Microsoft.AspNetCore.Mvc;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class CapturesController(ICaptureManager captureManager) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<TwitchCapture>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCaptures() => Ok(captureManager.Captures);
}
