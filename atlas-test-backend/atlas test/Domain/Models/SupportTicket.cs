namespace atlas_test.Domain.Models;

public sealed class SupportTicket
{
    public string TicketId { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string SubType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Resolution { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedDate { get; set; }

    public DateTimeOffset? ResolvedDate { get; set; }
}

