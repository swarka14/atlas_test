namespace atlas_test.Application.Configuration;

public sealed class DataOptions
{
    public const string SectionName = "Data";

    public string TicketsFilePath { get; set; } = "tickets.json";

    public string GoldenEvalFilePath { get; set; } = "golden_eval.json";
}

