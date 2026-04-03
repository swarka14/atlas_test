using atlas_test.Application.DTOs;
using atlas_test.Domain.Models;

namespace atlas_test.Application.Services;

public interface IChatService
{
    Task<ChatCompletionResult> GenerateAnswerAsync(string question, IReadOnlyCollection<RetrievedChunk> chunks, CancellationToken cancellationToken);

    Task<ChatCompletionResult> StreamAnswerAsync(
        string question,
        IReadOnlyCollection<RetrievedChunk> chunks,
        Func<string, Task> onToken,
        CancellationToken cancellationToken);

    Task<double> ScoreAnswerAsync(string question, string expectedAnswer, string generatedAnswer, CancellationToken cancellationToken);
}

