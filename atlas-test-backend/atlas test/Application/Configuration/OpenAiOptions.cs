namespace atlas_test.Application.Configuration;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string ChatModel { get; set; } = "gpt-4o-mini";

    public string JudgeModel { get; set; } = "gpt-4o-mini";
}

