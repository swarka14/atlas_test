using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using atlas_test.Application.Configuration;
using atlas_test.Application.Services;
using atlas_test.Domain.Models;
using Microsoft.Extensions.Options;

namespace atlas_test.Infrastructure.VectorDb;

public sealed class QdrantVectorStore : IVectorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly QdrantOptions _options;

    public QdrantVectorStore(IHttpClientFactory httpClientFactory, IOptions<QdrantOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var payload = new
        {
            vectors = new
            {
                size = _options.VectorSize,
                distance = _options.Distance
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"collections/{_options.CollectionName}")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Qdrant EnsureCollection failed ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task DeleteCollectionAsync(CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var response = await client.DeleteAsync($"collections/{_options.CollectionName}", cancellationToken);
        // 404 means it doesn't exist, which is fine
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Qdrant DeleteCollection failed ({(int)response.StatusCode}): {body}");
        }
    }

    public async Task UpsertChunksAsync(
        IReadOnlyCollection<TicketChunk> chunks,
        IReadOnlyCollection<float[]> embeddings,
        CancellationToken cancellationToken)
    {
        if (chunks.Count != embeddings.Count)
        {
            throw new ArgumentException("Chunks and embeddings count must match.");
        }

        var pointPayload = chunks.Zip(embeddings, (chunk, vector) => new
        {
            id = chunk.ChunkId,
            vector,
            payload = new
            {
                ticketId = chunk.TicketId,
                companyName = chunk.CompanyName,
                type = chunk.Type,
                fieldName = chunk.FieldName,
                chunkIndex = chunk.ChunkIndex,
                text = chunk.Text
            }
        }).ToArray();

        var body = new
        {
            points = pointPayload
        };

        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, $"collections/{_options.CollectionName}/points?wait=true")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Qdrant upsert failed ({(int)response.StatusCode}): {responseBody}");
        }
    }

    public async Task<IReadOnlyCollection<RetrievedChunk>> SearchAsync(
        float[] queryVector,
        int topK,
        string? companyName,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["vector"] = queryVector,
            ["limit"] = topK,
            ["with_payload"] = true
        };

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            body["filter"] = new
            {
                must = new[]
                {
                    new
                    {
                        key = "companyName",
                        match = new
                        {
                            value = companyName
                        }
                    }
                }
            };
        }

        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"collections/{_options.CollectionName}/points/search")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Qdrant search failed ({(int)response.StatusCode}): {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        if (!doc.RootElement.TryGetProperty("result", out var resultNode))
        {
            return Array.Empty<RetrievedChunk>();
        }

        var results = new List<RetrievedChunk>();
        foreach (var node in resultNode.EnumerateArray())
        {
            var payload = node.GetProperty("payload");
            results.Add(new RetrievedChunk
            {
                ChunkId = node.GetProperty("id").ToString(),
                TicketId = GetString(payload, "ticketId"),
                CompanyName = GetString(payload, "companyName"),
                Type = GetString(payload, "type"),
                FieldName = GetString(payload, "fieldName"),
                Text = GetString(payload, "text"),
                Score = node.TryGetProperty("score", out var scoreNode) ? scoreNode.GetDouble() : 0
            });
        }

        return results;
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        return client;
    }

    private static string GetString(JsonElement payload, string key)
    {
        return payload.TryGetProperty(key, out var value) ? value.ToString() : string.Empty;
    }
}

