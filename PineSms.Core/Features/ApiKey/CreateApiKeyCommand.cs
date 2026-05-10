namespace PineSms.Core.Features.ApiKey;

public class CreateApiKeyCommand
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateTime ExpireAt { get; set; }
}
