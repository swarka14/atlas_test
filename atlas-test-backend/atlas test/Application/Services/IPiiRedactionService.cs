namespace atlas_test.Application.Services;

public sealed class RedactionResult
{
    public string Text { get; init; } = string.Empty;

    public int EmailMatches { get; init; }

    public int PhoneMatches { get; init; }

    public int TotalMatches => EmailMatches + PhoneMatches;
}

public interface IPiiRedactionService
{
    RedactionResult Redact(string input);
}

