namespace TMS.Services;

public sealed class TicketMailOptions
{
    public const string SectionName = "Ticketing:Microsoft365";

    public bool Enabled { get; set; }
    public string MailboxAddress { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RequiredSubjectPrefix { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 60;
    public int InitialLookbackDays { get; set; } = 7;
    public int PageSize { get; set; } = 50;
    public int MaxPagesPerCycle { get; set; } = 20;

    public bool IsConfigured => Enabled
        && !string.IsNullOrWhiteSpace(MailboxAddress)
        && !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
