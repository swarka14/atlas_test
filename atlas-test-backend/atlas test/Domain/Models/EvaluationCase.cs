namespace atlas_test.Domain.Models;

public sealed class EvaluationCase
{
    public string QuestionId { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public string ExpectedAnswer { get; set; } = string.Empty;

    public IReadOnlyCollection<string> SourceTicketIds { get; set; } = Array.Empty<string>();
}

