namespace atlas_test.Application.DTOs;

public sealed class EvaluationQuestionResultDto
{
    public string QuestionId { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public IReadOnlyCollection<string> ExpectedTicketIds { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> RetrievedTicketIds { get; set; } = Array.Empty<string>();

    public double PrecisionAt5 { get; set; }

    public double RecallAt5 { get; set; }

    public double Correctness { get; set; }

    public double Confidence { get; set; }

    public bool IsLowConfidence { get; set; }

    public string GeneratedAnswer { get; set; } = string.Empty;
}

public sealed class EvaluationSummaryDto
{
    public double AvgPrecision { get; set; }

    public double AvgRecall { get; set; }

    public double AvgCorrectness { get; set; }

    public double AvgConfidence { get; set; }

    public int LowConfidenceCount { get; set; }
}

public sealed class EvaluationReportDto
{
    public IReadOnlyCollection<EvaluationQuestionResultDto> Results { get; set; } = Array.Empty<EvaluationQuestionResultDto>();

    public EvaluationSummaryDto Summary { get; set; } = new();
}

public sealed class ChunkSizeExperimentItemDto
{
    public int ChunkSize { get; set; }

    public double AvgPrecision { get; set; }

    public double AvgRecall { get; set; }

    public double AvgCorrectness { get; set; }
}

public sealed class ChunkSizeExperimentReportDto
{
    public IReadOnlyCollection<ChunkSizeExperimentItemDto> Results { get; set; } = Array.Empty<ChunkSizeExperimentItemDto>();
}

