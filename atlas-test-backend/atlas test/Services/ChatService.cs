using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using atlas_test.Application.Configuration;
using atlas_test.Application.DTOs;
using atlas_test.Application.Services;
using atlas_test.Domain.Models;
using Microsoft.Extensions.Options;

namespace atlas_test.Services;

public sealed partial class ChatService(
    IOpenAiClient openAiClient,
    IOptions<OpenAiOptions> openAiOptions) : IChatService
{
    public async Task<ChatCompletionResult> GenerateAnswerAsync(
        string question,
        IReadOnlyCollection<RetrievedChunk> chunks,
        CancellationToken cancellationToken)
    {
        var answerText = await openAiClient.CreateChatCompletionAsync(
            BuildSystemPrompt(),
            BuildUserPrompt(question, chunks),
            cancellationToken);

        return BuildCompletionResult(answerText, chunks);
    }

    public async Task<ChatCompletionResult> StreamAnswerAsync(
        string question,
        IReadOnlyCollection<RetrievedChunk> chunks,
        Func<string, Task> onToken,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        await openAiClient.StreamChatCompletionAsync(
            BuildSystemPrompt(),
            BuildUserPrompt(question, chunks),
            async token =>
            {
                sb.Append(token);
                await onToken(token);
            },
            cancellationToken);

        return BuildCompletionResult(sb.ToString(), chunks);
    }

    public async Task<double> ScoreAnswerAsync(
        string question,
        string expectedAnswer,
        string generatedAnswer,
        CancellationToken cancellationToken)
    {
        var systemPrompt = "You are a strict evaluator. Return JSON only: {\"score\": number}. The score must be between 0 and 1.";
        var userPrompt = $"""
Compare the generated answer to the expected answer.
Question: {question}
Expected Answer: {expectedAnswer}
Generated Answer: {generatedAnswer}

Scoring rubric:
- 1.0: fully correct and complete
- 0.7-0.9: mostly correct with minor misses
- 0.4-0.6: partially correct
- 0.1-0.3: mostly incorrect
- 0.0: incorrect or irrelevant
""";

        var judgeOutput = await openAiClient.CreateChatCompletionAsync(
            systemPrompt,
            userPrompt,
            cancellationToken,
            openAiOptions.Value.JudgeModel);

        return ParseJudgeScore(judgeOutput);
    }

    private static ChatCompletionResult BuildCompletionResult(string answerText, IReadOnlyCollection<RetrievedChunk> chunks)
    {
        var confidence = ExtractConfidence(answerText);
        var cleanAnswer = ConfidenceRegex().Replace(answerText, "").TrimEnd();

        return new ChatCompletionResult
        {
            Answer = cleanAnswer,
            TicketIds = ExtractTicketIds(cleanAnswer, chunks),
            Confidence = confidence
        };
    }

    private static double ExtractConfidence(string text)
    {
        var match = ConfidenceRegex().Match(text);
        if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
        {
            return Math.Clamp(score, 0, 1);
        }

        return 1.0;
    }

    private static double ParseJudgeScore(string judgeOutput)
    {
        try
        {
            using var doc = JsonDocument.Parse(judgeOutput);
            if (doc.RootElement.TryGetProperty("score", out var scoreNode) && scoreNode.TryGetDouble(out var score))
            {
                return Math.Clamp(score, 0, 1);
            }
        }
        catch
        {
            // Fallback parsing below.
        }

        var numberMatch = NumberRegex().Match(judgeOutput);
        if (numberMatch.Success && double.TryParse(numberMatch.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return Math.Clamp(parsed, 0, 1);
        }

        return 0;
    }

    private static string BuildSystemPrompt()
    {
        return """
You are an IT support assistant using retrieved ticket context.
Rules:
1) Answer only from provided context.
2) If context is insufficient, say so.
3) Include citations as [ticketId] in the answer.
4) Do not hallucinate facts.
5) End your response with [CONFIDENCE: X.X] where X.X is your confidence level between 0.0 and 1.0 that the answer is correct and complete based on the provided context.
""";
    }

    private static string BuildUserPrompt(string question, IReadOnlyCollection<RetrievedChunk> chunks)
    {
        var context = string.Join(
            "\n\n",
            chunks.Select(c =>
                $"[ticketId={c.TicketId}; company={c.CompanyName}; type={c.Type}; field={c.FieldName}; score={c.Score:F3}]\n{c.Text}"));

        return $"""
Question:
{question}

Retrieved Context:
{context}

Return a concise answer with ticket citations.
""";
    }

    private static IReadOnlyCollection<string> ExtractTicketIds(string answer, IReadOnlyCollection<RetrievedChunk> chunks)
    {
        var candidates = chunks.Select(c => c.TicketId).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ids = TicketRegex().Matches(answer).Select(m => m.Value).Where(candidates.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (ids.Length > 0)
        {
            return ids;
        }

        return chunks.Select(c => c.TicketId).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToArray();
    }

    [GeneratedRegex("TKT-\\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TicketRegex();

    [GeneratedRegex("(?:0(?:\\.\\d+)?)|(?:1(?:\\.0+)?)", RegexOptions.Compiled)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"\[CONFIDENCE:\s*(\d+\.?\d*)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ConfidenceRegex();
}


