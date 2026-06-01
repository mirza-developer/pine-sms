using System.Collections.Concurrent;
using PineSms.Core.Dtos;

namespace PineSms.UI.Services;

/// <summary>
/// Singleton store that lets a Blazor component hand off a list of phone numbers
/// to the /download/customers-excel/{token} minimal API endpoint without using JS.
/// Tokens are one-time-use and expire after 5 minutes.
/// </summary>
public class ExcelDownloadTokenStore
{
    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new();

    public string CreateToken(List<string> phoneNumbers)
    {
        Purge();
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = new TokenEntry { Phones = phoneNumbers, CreatedAt = DateTime.UtcNow };
        return token;
    }

    public List<string>? Consume(string token)
    {
        if (_tokens.TryRemove(token, out var entry))
            return entry.Phones;
        return null;
    }

    private void Purge()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        foreach (var key in _tokens.Keys)
        {
            if (_tokens.TryGetValue(key, out var entry) && entry.CreatedAt < cutoff)
                _tokens.TryRemove(key, out _);
        }
    }
}
