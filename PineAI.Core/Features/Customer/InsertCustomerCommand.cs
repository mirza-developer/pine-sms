namespace PineAI.Core.Features.Customer;

public class InsertCustomerCommand
{
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? Gender { get; set; }
    public int? BirthYear { get; set; }
    public string? BirthDate { get; set; }
    /// <summary>When true, this customer receives SMS in every sending chunk regardless of selection.</summary>
    public bool IsTester { get; set; } = false;
}
