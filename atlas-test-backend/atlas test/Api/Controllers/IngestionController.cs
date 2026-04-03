using atlas_test.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace atlas_test.Api.Controllers;

[ApiController]
[Route("api/ingest")]
public sealed class IngestionController(IIngestionService ingestionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Ingest(CancellationToken cancellationToken)
    {
        var result = await ingestionService.IngestAsync(cancellationToken);
        return Ok(result);
    }
}

