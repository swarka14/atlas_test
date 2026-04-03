using atlas_test.Domain.Models;

namespace atlas_test.Application.Services;

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken);

    Task DeleteCollectionAsync(CancellationToken cancellationToken);

    Task UpsertChunksAsync(IReadOnlyCollection<TicketChunk> chunks, IReadOnlyCollection<float[]> embeddings, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RetrievedChunk>> SearchAsync(float[] queryVector, int topK, string? companyName, CancellationToken cancellationToken);
}

