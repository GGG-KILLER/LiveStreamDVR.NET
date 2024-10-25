using Microsoft.AspNetCore.Mvc;
using Tenray.ZoneTree;

namespace LiveStreamDVR.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class DatabaseController(IZoneTree<string, string> database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<KeyValuePair<string, string>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IEnumerable<KeyValuePair<string, string>> GetAllEntries()
    {
        var iterator = database.CreateIterator(IteratorType.Snapshot);
        while (iterator.Next())
        {
            if (iterator.CurrentKey.Contains("secret", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return new KeyValuePair<string, string>(iterator.CurrentKey, iterator.CurrentValue);
        }
    }

    [HttpGet("{key:required}")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public IActionResult GetEntry(string key)
    {
        if (key.Contains("secret", StringComparison.OrdinalIgnoreCase))
            return Forbid();

        if (database.TryGet(key, out var value))
            return Ok(value);
        return NotFound();
    }

    [HttpPost("{key:required}")]
    [ProducesResponseType<long>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult SetEntry(string key, [FromBody] string content)
    {
        return Ok(database.Upsert(key, content));
    }
}
