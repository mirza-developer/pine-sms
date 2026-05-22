using System.Collections.Concurrent;

namespace PineSms.BaleBot.Services;

/// <summary>
/// Thread-safe in-memory store that remembers the most recent photo message sent
/// by each Bale user.  Used to forward the photo to support staff when the AI
/// generates a FEEDBACK block that includes <c>HasPhoto: true</c>.
/// </summary>
public class PhotoMessageStore
{
    private readonly ConcurrentDictionary<long, (long MessageId, long FromChatId)> photos = new();

    /// <summary>Stores (or replaces) the latest photo message for the given user chat.</summary>
    public void StorePhoto(long userChatId, long messageId)
        => photos[userChatId] = (messageId, userChatId);

    /// <summary>
    /// Removes and returns the stored photo for the given user chat, or <c>null</c> if none exists.
    /// </summary>
    public (long MessageId, long FromChatId)? TakePhoto(long userChatId)
    {
        if (photos.TryRemove(userChatId, out var photo))
            return photo;
        return null;
    }

    /// <summary>Returns the stored photo for the given user chat without removing it, or <c>null</c> if none exists.</summary>
    public (long MessageId, long FromChatId)? PeekPhoto(long userChatId)
    {
        if (photos.TryGetValue(userChatId, out var photo))
            return photo;
        return null;
    }
}
