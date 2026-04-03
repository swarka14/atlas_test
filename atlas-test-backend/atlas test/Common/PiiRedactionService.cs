using System.Text.RegularExpressions;
using atlas_test.Application.Services;

namespace atlas_test.Common;

public sealed class PiiRedactionService(ILogger<PiiRedactionService> logger) : IPiiRedactionService
{
    private static readonly Regex EmailRegex = new(
        "[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        "(?<!\\w)(?:\\+?\\d{1,3}[\\s.-]?)?(?:\\(\\d{3}\\)|\\d{3})[\\s.-]?\\d{3}[\\s.-]?\\d{4}(?!\\w)",
        RegexOptions.Compiled);

    public RedactionResult Redact(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new RedactionResult { Text = input };
        }

        var emailMatches = EmailRegex.Matches(input).Count;
        var emailRedacted = EmailRegex.Replace(input, "[REDACTED]");

        var phoneMatches = PhoneRegex.Matches(emailRedacted).Count;
        var fullyRedacted = PhoneRegex.Replace(emailRedacted, "[REDACTED]");

        if (emailMatches + phoneMatches > 0)
        {
            logger.LogInformation("PII redaction applied. Emails: {EmailCount}, Phones: {PhoneCount}", emailMatches, phoneMatches);
        }

        return new RedactionResult
        {
            Text = fullyRedacted,
            EmailMatches = emailMatches,
            PhoneMatches = phoneMatches
        };
    }
}

