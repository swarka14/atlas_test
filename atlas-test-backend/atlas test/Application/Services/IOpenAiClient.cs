namespace atlas_test.Application.Services;

public interface IOpenAiClient
{
    Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken);

    Task<string> CreateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken,
        string? model = null);

    Task StreamChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        Func<string, Task> onToken,
        CancellationToken cancellationToken,
        string? model = null);
}

