using System.ComponentModel.DataAnnotations;

namespace PineAI.Core.Entities;

/// <summary>
/// Stores a single message exchanged between a Bale user and the bot.
/// </summary>
public class BotChatMessage : IBaseEntity
{
    [Key]
    public int Id { get; set; }

    /// <summary>Bale messenger username of the user (without @). May equal ChatId.ToString() when the user has no username.</summary>
    [Required]
    [StringLength(64)]
    public string BaleUsername { get; set; } = string.Empty;

    /// <summary>Bale chat ID of the conversation.</summary>
    public long ChatId { get; set; }

    /// <summary>Text content of the message.</summary>
    [Required]
    public string MessageText { get; set; } = string.Empty;

    /// <summary>False = sent by the user; True = sent by the bot.</summary>
    public bool IsFromBot { get; set; }

    /// <summary>UTC date/time when the message was sent.</summary>
    public DateTime SentAt { get; set; }
}
