namespace atlas_test.Domain.Models;

public sealed class TicketChunk
{
    public string ChunkId { get; set; } = Guid.NewGuid().ToString("N");

    public string TicketId { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string FieldName { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Text { get; set; } = string.Empty;
}

