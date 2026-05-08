namespace PineSms.Core.Entities;

public class CustomerOrder : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string OrderCode { get; set; } = string.Empty;

    [Required]
    public int OrderStatusId { get; set; }

    public OrderStatus OrderStatus { get; set; } = null!;

    [Required]
    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    [StringLength(128)]
    public string? PostalTrackingCode { get; set; }
}
