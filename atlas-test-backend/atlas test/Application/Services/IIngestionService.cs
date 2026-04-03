using atlas_test.Application.DTOs;

namespace atlas_test.Application.Services;

public interface IIngestionService
{
    Task<IngestionResultDto> IngestAsync(CancellationToken cancellationToken, int? chunkSize = null, int? chunkOverlap = null);
}

