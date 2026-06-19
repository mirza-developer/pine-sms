namespace PineAI.Core.Entities;

public class OrderStatus : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime LastChange { get; set; }

    public ICollection<CustomerOrder> CustomerOrders { get; set; } = new List<CustomerOrder>();
}
