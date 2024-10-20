using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class DebugController(IConfiguration configuration, HttpClient httpClient) : ControllerBase
{
    [HttpGet("OpenIdConfiguration")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOpenIdConfigurationAsync()
    {
        var response = await httpClient.GetFromJsonAsync<JsonDocument>(configuration["Authentication:Schemes:Bearer:MetadataAddress"]);
        return Ok(response);
    }
}
