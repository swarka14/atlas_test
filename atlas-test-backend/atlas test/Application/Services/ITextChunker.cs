using atlas_test.Domain.Models;

namespace atlas_test.Application.Services;

public interface ITextChunker
{
    IReadOnlyCollection<TicketChunk> ChunkTicket(SupportTicket ticket, int chunkSize, int overlap);
}

