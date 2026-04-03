using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using atlas_test.Application.Configuration;
using atlas_test.Application.Services;
using Microsoft.Extensions.Options;

namespace atlas_test.Infrastructure.OpenAI;

public sealed class OpenAiClient : IOpenAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _options;

    public OpenAiClient(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var responseBody = await SendJsonAsync(
            "embeddings",
            new
            {
                model = _options.EmbeddingModel,
                input = text
            },
            cancellationToken);

        using var doc = JsonDocument.Parse(responseBody);
        var embeddingJson = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

        var vector = new float[embeddingJson.GetArrayLength()];
        var index = 0;
        foreach (var item in embeddingJson.EnumerateArray())
        {
            vector[index] = item.GetSingle();
            index++;
        }

        return vector;
    }

    public async Task<string> CreateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        string? model = null)
    {
        var payload = BuildChatPayload(systemPrompt, userPrompt, stream: false, model);
        var responseBody = await SendJsonAsync("chat/completions", payload, cancellationToken);

        using var response = JsonDocument.Parse(responseBody);
        return response.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async Task StreamChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        Func<string, Task> onToken,
        CancellationToken cancellationToken,
        string? model = null)
    {
        var payload = BuildChatPayload(systemPrompt, userPrompt, stream: true, model);
        var client = CreateClient();
        using var request = CreateJsonRequest("chat/completions", payload);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessStatusAsync(response, "OpenAI chat stream", cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            var token = ParseStreamToken(line);
            if (token == "[DONE]")
            {
                break;
            }

            if (!string.IsNullOrEmpty(token))
            {
                await onToken(token);
            }
        }
    }

    private object BuildChatPayload(string systemPrompt, string userPrompt, bool stream, string? model)
    {
        var selectedModel = string.IsNullOrWhiteSpace(model) ? _options.ChatModel : model;
        return new
        {
            model = selectedModel,
            temperature = 0.2,
            stream,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };
    }

    private async Task<string> SendJsonAsync(string endpoint, object payload, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var request = CreateJsonRequest(endpoint, payload);

        using var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccessStatusAsync(response, "OpenAI request", cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static HttpRequestMessage CreateJsonRequest(string endpoint, object payload)
    {
        return new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
    }

    private static async Task EnsureSuccessStatusAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"{operation} failed ({(int)response.StatusCode}): {body}");
    }

    private static string? ParseStreamToken(string? line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
        {
            return null;
        }

        var data = line[6..].Trim();
        if (data == "[DONE]")
        {
            return "[DONE]";
        }

        using var payload = JsonDocument.Parse(data);
        var choices = payload.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
        {
            return null;
        }

        var delta = choices[0].GetProperty("delta");
        return delta.TryGetProperty("content", out var contentNode) ? contentNode.GetString() : null;
    }

    private HttpClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Set OpenAI:ApiKey or OPENAI__APIKEY.");
        }

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}


