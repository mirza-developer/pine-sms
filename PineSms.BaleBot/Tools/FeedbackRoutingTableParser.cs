using System.Text.RegularExpressions;

namespace PineSms.BaleBot.Tools;

/// <summary>
/// Parses the feedback routing table from the markdown instructions file
/// to build a dynamic routing configuration.
/// </summary>
public static class FeedbackRoutingTableParser
{
    /// <summary>
    /// Parses the markdown file and extracts the feedback routing table.
    /// Expected format in markdown:
    /// | # | Feedback Type | When to Use | Required Fields | Target Chat ID |
    /// | 1 | `Satisfaction` | ... | ... | 4675184120 |
    /// </summary>
    /// <param name="markdownFilePath">Full path to the markdown instructions file</param>
    /// <returns>Dictionary mapping feedback type names to chat IDs</returns>
    public static Dictionary<string, long> ParseRoutingTable(string markdownFilePath)
    {
        var routingTable = new Dictionary<string, long>();

        if (!File.Exists(markdownFilePath))
        {
            throw new FileNotFoundException($"Markdown file not found: {markdownFilePath}");
        }

        var content = File.ReadAllText(markdownFilePath);

        // Find the feedback routing table section
        // Pattern: | # | Feedback Type | ... | Target Chat ID |
        // Match table rows with feedback type and chat ID
        var tableRowPattern = @"\|\s*\d+\s*\|\s*`([^`]+)`\s*\|[^|]+\|[^|]+\|\s*(\d+)\s*\|";
        var matches = Regex.Matches(content, tableRowPattern);

        foreach (Match match in matches)
        {
            if (match.Groups.Count >= 3)
            {
                var feedbackType = match.Groups[1].Value.Trim();
                var chatIdStr = match.Groups[2].Value.Trim();

                if (long.TryParse(chatIdStr, out long chatId))
                {
                    routingTable[feedbackType] = chatId;
                }
            }
        }

        if (routingTable.Count == 0)
        {
            throw new InvalidOperationException("No feedback routing entries found in markdown file");
        }

        return routingTable;
    }

    /// <summary>
    /// Gets the path to the markdown instructions file relative to the application base directory.
    /// </summary>
    /// <returns>Full path to chtbot-instructions-main.md</returns>
    public static string GetMarkdownFilePath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, "Chat", "chtbot-instructions-main.md");
    }
}
