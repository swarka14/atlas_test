namespace atlas_test.Application.DTOs;

public sealed class IngestionResultDto
{
    public int TicketsProcessed { get; set; }

    public int ChunksStored { get; set; }

    public int RedactionCount { get; set; }
}

