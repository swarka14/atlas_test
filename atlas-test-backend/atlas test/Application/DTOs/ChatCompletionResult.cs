namespace atlas_test.Application.DTOs;

public sealed class ChatCompletionResult
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyCollection<string> TicketIds { get; set; } = Array.Empty<string>();

    public double Confidence { get; set; } = 1.0;
}

