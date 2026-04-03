using atlas_test.Application.Services;
using atlas_test.Domain.Models;

namespace atlas_test.Common;

public sealed class TextChunker : ITextChunker
{
    public IReadOnlyCollection<TicketChunk> ChunkTicket(SupportTicket ticket, int chunkSize, int overlap)
    {
        var chunks = new List<TicketChunk>();

        AddFieldChunks(chunks, ticket, "description", ticket.Description, chunkSize, overlap);
        AddFieldChunks(chunks, ticket, "resolution", ticket.Resolution, chunkSize, overlap);
        AddFieldChunks(chunks, ticket, "notes", ticket.Notes, chunkSize, overlap);

        return chunks;
    }

    private static void AddFieldChunks(
        ICollection<TicketChunk> chunks,
        SupportTicket ticket,
        string fieldName,
        string value,
        int chunkSize,
        int overlap)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();
        if (normalized.Length <= chunkSize)
        {
            chunks.Add(BuildChunk(ticket, fieldName, normalized, 0));
            return;
        }

        var step = Math.Max(1, chunkSize - overlap);
        var index = 0;
        for (var start = 0; start < normalized.Length; start += step)
        {
            var len = Math.Min(chunkSize, normalized.Length - start);
            var chunk = normalized.Substring(start, len).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(BuildChunk(ticket, fieldName, chunk, index));
                index++;
            }

            if (start + len >= normalized.Length)
            {
                break;
            }
        }
    }

    private static TicketChunk BuildChunk(SupportTicket ticket, string fieldName, string text, int chunkIndex)
    {
        return new TicketChunk
        {
            ChunkId = Guid.NewGuid().ToString("N"),
            TicketId = ticket.TicketId,
            CompanyName = ticket.CompanyName,
            Type = ticket.Type,
            FieldName = fieldName,
            ChunkIndex = chunkIndex,
            Text = text
        };
    }
}

