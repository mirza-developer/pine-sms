namespace PineAI.Core.Entities;

public class ApiKey : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public DateTime ExpireAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }
}
