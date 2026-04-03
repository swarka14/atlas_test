using atlas_test.Application.DTOs;
using atlas_test.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace atlas_test.Api.Controllers;

[ApiController]
[Route("api/evaluate")]
public sealed class EvaluationController(IEvaluationService evaluationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Evaluate(CancellationToken cancellationToken)
    {
        var result = await evaluationService.RunAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("chunk-experiment")]
    public async Task<IActionResult> ChunkExperiment([FromBody] ChunkSizeExperimentRequestDto? request, CancellationToken cancellationToken)
    {
        var chunkSizes = request?.ChunkSizes ?? [500, 1000, 2000];
        var result = await evaluationService.RunChunkSizeExperimentAsync(chunkSizes, cancellationToken);
        return Ok(result);
    }
}

