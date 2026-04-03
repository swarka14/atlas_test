using atlas_test.Application.DTOs;

namespace atlas_test.Application.Services;

public interface IEvaluationService
{
    Task<EvaluationReportDto> RunAsync(CancellationToken cancellationToken);

    Task<ChunkSizeExperimentReportDto> RunChunkSizeExperimentAsync(
        IReadOnlyCollection<int> chunkSizes,
        CancellationToken cancellationToken);
}

