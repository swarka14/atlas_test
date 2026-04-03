using System.Text.Json;
using atlas_test.Application.Configuration;
using atlas_test.Application.DTOs;
using atlas_test.Application.Services;
using atlas_test.Domain.Models;
using Microsoft.Extensions.Options;

namespace atlas_test.Services;

public sealed class IngestionService(
    ITextChunker textChunker,
    IPiiRedactionService redactionService,
    IOpenAiClient openAiClient,
    IVectorStore vectorStore,
    IOptions<DataOptions> dataOptions,
    IOptions<RetrievalOptions> retrievalOptions,
    ILogger<IngestionService> logger) : IIngestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IngestionResultDto> IngestAsync(
        CancellationToken cancellationToken,
        int? chunkSize = null,
        int? chunkOverlap = null)
    {
        var tickets = await LoadTicketsAsync(cancellationToken);

        await vectorStore.DeleteCollectionAsync(cancellationToken);
        await vectorStore.EnsureCollectionAsync(cancellationToken);

        var effectiveChunkSize = chunkSize ?? retrievalOptions.Value.ChunkSize;
        var effectiveChunkOverlap = chunkOverlap ?? retrievalOptions.Value.ChunkOverlap;
        var (chunksToStore, redactionCount) = BuildChunksWithRedaction(tickets, effectiveChunkSize, effectiveChunkOverlap);

        var embeddings = await CreateEmbeddingsAsync(chunksToStore, cancellationToken);

        if (chunksToStore.Count > 0)
        {
            await vectorStore.UpsertChunksAsync(chunksToStore, embeddings, cancellationToken);
        }

        return new IngestionResultDto
        {
            TicketsProcessed = tickets.Count,
            ChunksStored = chunksToStore.Count,
            RedactionCount = redactionCount
        };
    }

    private async Task<List<SupportTicket>> LoadTicketsAsync(CancellationToken cancellationToken)
    {
        var path = ResolveDataPath(dataOptions.Value.TicketsFilePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Tickets file was not found at: {path}");
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<SupportTicket>>(stream, JsonOptions, cancellationToken)
               ?? new List<SupportTicket>();
    }

    private (List<TicketChunk> Chunks, int RedactionCount) BuildChunksWithRedaction(
        IReadOnlyCollection<SupportTicket> tickets,
        int chunkSize,
        int chunkOverlap)
    {
        var chunks = new List<TicketChunk>();
        var redactionCount = 0;

        foreach (var ticket in tickets)
        {
            var ticketChunks = textChunker.ChunkTicket(ticket, chunkSize, chunkOverlap);
            foreach (var chunk in ticketChunks)
            {
                var redaction = redactionService.Redact(chunk.Text);
                if (redaction.TotalMatches > 0)
                {
                    logger.LogInformation(
                        "Ticket {TicketId} chunk {ChunkId} was redacted ({Matches} matches)",
                        chunk.TicketId,
                        chunk.ChunkId,
                        redaction.TotalMatches);
                }

                redactionCount += redaction.TotalMatches;
                chunk.Text = redaction.Text;
                chunks.Add(chunk);
            }
        }

        return (chunks, redactionCount);
    }

    private async Task<List<float[]>> CreateEmbeddingsAsync(
        IReadOnlyCollection<TicketChunk> chunks,
        CancellationToken cancellationToken)
    {
        var vectors = new List<float[]>(chunks.Count);
        foreach (var chunk in chunks)
        {
            vectors.Add(await openAiClient.CreateEmbeddingAsync(chunk.Text, cancellationToken));
        }

        return vectors;
    }

    private static string ResolveDataPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }
}

