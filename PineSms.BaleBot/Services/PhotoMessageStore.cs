using System.Collections.Concurrent;

namespace PineSms.BaleBot.Services;

/// <summary>
/// Thread-safe in-memory store that remembers the most recent photo message sent
/// by each Bale user.  Used to forward the photo to support staff when the AI
/// generates a FEEDBACK block that includes <c>HasPhoto: true</c>.
/// </summary>
public class PhotoMessageStore
{
    private readonly ConcurrentDictionary<long, long> photos = new();

    /// <summary>Stores (or replaces) the latest photo message ID for the given user chat.</summary>
    public void StorePhoto(long userChatId, long messageId)
        => photos[userChatId] = messageId;

    /// <summary>
    /// Removes and returns the stored photo message ID for the given user chat,
    /// or <c>null</c> if none exists.
    /// </summary>
    public long? TakePhoto(long userChatId)
    {
        if (photos.TryRemove(userChatId, out var messageId))
            return messageId;
        return null;
    }

    /// <summary>Returns the stored photo message ID for the given user chat without removing it, or <c>null</c> if none exists.</summary>
    public long? PeekPhoto(long userChatId)
    {
        if (photos.TryGetValue(userChatId, out var messageId))
            return messageId;
        return null;
    }
}
