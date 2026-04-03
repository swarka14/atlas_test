using System.Text.Json;
using atlas_test.Application.Configuration;
using atlas_test.Application.DTOs;
using atlas_test.Application.Services;
using atlas_test.Domain.Models;
using Microsoft.Extensions.Options;

namespace atlas_test.Services;

public sealed class EvaluationService(
    IIngestionService ingestionService,
    IRetrievalService retrievalService,
    IChatService chatService,
    IOptions<DataOptions> dataOptions,
    IOptions<RetrievalOptions> retrievalOptions,
    ILogger<EvaluationService> logger) : IEvaluationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EvaluationReportDto> RunAsync(CancellationToken cancellationToken)
    {
        var evalCases = await ReadCasesAsync(cancellationToken);
        return await RunInternalAsync(evalCases, cancellationToken);
    }

    public async Task<ChunkSizeExperimentReportDto> RunChunkSizeExperimentAsync(
        IReadOnlyCollection<int> chunkSizes,
        CancellationToken cancellationToken)
    {
        var validSizes = NormalizeChunkSizes(chunkSizes);

        var evalCases = await ReadCasesAsync(cancellationToken);
        var results = new List<ChunkSizeExperimentItemDto>(validSizes.Length);

        foreach (var size in validSizes)
        {
            // Rebuild vector data for each chunk size before evaluating retrieval quality.
            await ingestionService.IngestAsync(cancellationToken, chunkSize: size, chunkOverlap: Math.Max(50, size / 8));
            var report = await RunInternalAsync(evalCases, cancellationToken);

            results.Add(new ChunkSizeExperimentItemDto
            {
                ChunkSize = size,
                AvgPrecision = report.Summary.AvgPrecision,
                AvgRecall = report.Summary.AvgRecall,
                AvgCorrectness = report.Summary.AvgCorrectness
            });
        }

        return new ChunkSizeExperimentReportDto
        {
            Results = results
        };
    }

    private async Task<EvaluationReportDto> RunInternalAsync(
        IReadOnlyCollection<EvaluationCase> evalCases,
        CancellationToken cancellationToken)
    {
        var results = new List<EvaluationQuestionResultDto>(evalCases.Count);

        foreach (var evalCase in evalCases)
        {
            var result = await EvaluateCaseAsync(evalCase, cancellationToken);
            results.Add(result);

            logger.LogInformation(
                "Eval {QuestionId}: P@5={Precision} R@5={Recall} Correctness={Correctness}",
                result.QuestionId,
                result.PrecisionAt5,
                result.RecallAt5,
                result.Correctness);
        }

        return new EvaluationReportDto
        {
            Results = results,
            Summary = BuildSummary(results)
        };
    }

    private async Task<EvaluationQuestionResultDto> EvaluateCaseAsync(
        EvaluationCase evalCase,
        CancellationToken cancellationToken)
    {
        var retrieved = await retrievalService.RetrieveAsync(evalCase.Question, companyName: null, cancellationToken);
        var topFive = retrieved.Take(5).ToArray();

        var hitCount = topFive.Count(c => evalCase.SourceTicketIds.Contains(c.TicketId, StringComparer.OrdinalIgnoreCase));
        var precisionAt5 = hitCount / 5d;

        var retrievedTicketIds = topFive.Select(c => c.TicketId).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var relevantFound = evalCase.SourceTicketIds.Count(retrievedTicketIds.Contains);
        var recallAt5 = evalCase.SourceTicketIds.Count > 0
            ? (double)relevantFound / evalCase.SourceTicketIds.Count
            : 0;
        // Generate an answer using the retrieved top-five chunks, then score it.
        var answer = await chatService.GenerateAnswerAsync(evalCase.Question, topFive, cancellationToken);

        var correctness = await chatService.ScoreAnswerAsync(
            evalCase.Question,
            evalCase.ExpectedAnswer,
            answer.Answer,
            cancellationToken);

        return new EvaluationQuestionResultDto
        {
            QuestionId = evalCase.QuestionId,
            Question = evalCase.Question,
            ExpectedTicketIds = evalCase.SourceTicketIds.ToArray(),
            RetrievedTicketIds = topFive.Select(c => c.TicketId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            PrecisionAt5 = precisionAt5,
            RecallAt5 = recallAt5,
            Correctness = correctness,
            Confidence = answer.Confidence,
            IsLowConfidence = answer.Confidence < retrievalOptions.Value.ConfidenceThreshold,
            GeneratedAnswer = answer.Answer
        };
    }

    private static EvaluationSummaryDto BuildSummary(IReadOnlyCollection<EvaluationQuestionResultDto> results)
    {
        if (results.Count == 0)
        {
            return new EvaluationSummaryDto();
        }

        return new EvaluationSummaryDto
        {
            AvgPrecision = results.Average(r => r.PrecisionAt5),
            AvgRecall = results.Average(r => r.RecallAt5),
            AvgCorrectness = results.Average(r => r.Correctness),
            AvgConfidence = results.Average(r => r.Confidence),
            LowConfidenceCount = results.Count(r => r.IsLowConfidence)
        };
    }

    private static int[] NormalizeChunkSizes(IReadOnlyCollection<int> chunkSizes)
    {
        var validSizes = chunkSizes.Where(x => x > 0).Distinct().OrderBy(x => x).ToArray();
        return validSizes.Length == 0 ? [500, 1000, 2000] : validSizes;
    }

    private async Task<List<EvaluationCase>> ReadCasesAsync(CancellationToken cancellationToken)
    {
        var path = ResolveDataPath(dataOptions.Value.GoldenEvalFilePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Golden eval file was not found at: {path}");
        }

        await using var stream = File.OpenRead(path);
        var raw = await JsonSerializer.DeserializeAsync<List<GoldenEvalRow>>(stream, JsonOptions, cancellationToken)
                  ?? new List<GoldenEvalRow>();

        return raw.Select(x => new EvaluationCase
        {
            QuestionId = x.QuestionId,
            Question = x.Question,
            ExpectedAnswer = x.ExpectedAnswer,
            SourceTicketIds = x.SourceTicketIds
        }).ToList();
    }

    private static string ResolveDataPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }

    private sealed class GoldenEvalRow
    {
        public string QuestionId { get; set; } = string.Empty;

        public string Question { get; set; } = string.Empty;

        public string ExpectedAnswer { get; set; } = string.Empty;

        public List<string> SourceTicketIds { get; set; } = [];
    }
}

