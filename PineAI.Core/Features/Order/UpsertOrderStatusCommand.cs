namespace PineAI.Core.Features.Order;

public class UpsertOrderStatusCommand
{
    public int? Id { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Title { get; set; } = string.Empty;
}
