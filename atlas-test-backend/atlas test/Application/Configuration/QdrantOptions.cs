namespace atlas_test.Application.Configuration;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string BaseUrl { get; set; } = "http://qdrant:6333";

    public string CollectionName { get; set; } = "it_tickets";

    public int VectorSize { get; set; } = 1536;

    public string Distance { get; set; } = "Cosine";
}

