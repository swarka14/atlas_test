using atlas_test.Domain.Models;

namespace atlas_test.Application.Services;

public interface IRetrievalService
{
    Task<IReadOnlyCollection<RetrievedChunk>> RetrieveAsync(string query, string? companyName, CancellationToken cancellationToken);
}

