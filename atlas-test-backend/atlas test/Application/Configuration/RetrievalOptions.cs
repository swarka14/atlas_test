namespace atlas_test.Application.Configuration;

public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int TopK { get; set; } = 5;

    public int ChunkSize { get; set; } = 900;

    public int ChunkOverlap { get; set; } = 120;

    public double ConfidenceThreshold { get; set; } = 0.6;
}

