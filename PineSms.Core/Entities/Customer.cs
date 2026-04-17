namespace PineSms.Core.Entities;

public class Customer : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public DateTime SaveDate { get; set; }

    [Required]
    public string SaveUserId { get; set; } = string.Empty;

    [Required]
    public int SaveType { get; set; } // 1 = Form, 2 = Excel

    [StringLength(128)]
    public string? Name { get; set; }

    public int? Gender { get; set; } // 1=Male, 2=Female

    public int? BirthYear { get; set; }

    public DateTime? BirthDate { get; set; }

    public DateTime? LastUsageDate { get; set; }
}
