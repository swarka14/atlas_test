using atlas_test.Application.Configuration;
using atlas_test.Application.Services;
using atlas_test.Domain.Models;
using Microsoft.Extensions.Options;

namespace atlas_test.Services;

public sealed class RetrievalService(
    IOpenAiClient openAiClient,
    IVectorStore vectorStore,
    IOptions<RetrievalOptions> retrievalOptions) : IRetrievalService
{
    public async Task<IReadOnlyCollection<RetrievedChunk>> RetrieveAsync(
        string query,
        string? companyName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<RetrievedChunk>();
        }

        var queryEmbedding = await openAiClient.CreateEmbeddingAsync(query, cancellationToken);
        return await vectorStore.SearchAsync(queryEmbedding, retrievalOptions.Value.TopK, companyName, cancellationToken);
    }
}

