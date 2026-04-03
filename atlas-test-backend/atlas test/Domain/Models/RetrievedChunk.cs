namespace atlas_test.Domain.Models;

public sealed class RetrievedChunk
{
    public string ChunkId { get; set; } = string.Empty;

    public string TicketId { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public double Score { get; set; }
}

