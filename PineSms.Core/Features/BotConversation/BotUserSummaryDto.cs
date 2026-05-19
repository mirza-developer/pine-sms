namespace PineSms.Core.Features.BotConversation;

/// <summary>Summary row shown in the paginated user grid on the admin conversations page.</summary>
public class BotUserSummaryDto
{
    public string BaleUsername { get; set; } = string.Empty;
    public long ChatId { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}

/// <summary>Paged result wrapper for <see cref="BotUserSummaryDto"/>.</summary>
public class BotUserSummaryPageResult
{
    public List<BotUserSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
