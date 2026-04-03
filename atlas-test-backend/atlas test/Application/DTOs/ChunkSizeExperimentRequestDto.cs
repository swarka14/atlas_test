namespace atlas_test.Application.DTOs;

public sealed class ChunkSizeExperimentRequestDto
{
    public IReadOnlyCollection<int> ChunkSizes { get; set; } = [500, 1000, 2000];
}

