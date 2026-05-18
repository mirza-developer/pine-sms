namespace PineSms.BaleBot.Tools;

/// <summary>
/// Utility for parsing structured command blocks embedded in AI agent responses.
/// The AI can embed blocks like:
/// <code>
/// &lt;&lt;ORDER_CODE
/// ORD-12345
/// &gt;&gt;
/// </code>
/// This class strips those blocks from the visible response text and optionally
/// collects the extracted values so the application can act on them.
/// </summary>
public static class ResponseBlockTools
{
    private const string OrderCodeStart = "<<ORDER_CODE";
    private const string FeedbackStart = "<<FEEDBACK";
    private const string BlockEnd = ">>";

    /// <summary>
    /// Strips all <c>&lt;&lt;ORDER_CODE … &gt;&gt;</c> blocks from <paramref name="text"/>.
    /// Extracted order codes are appended to <paramref name="collectedOrderCodes"/> when provided.
    /// </summary>
    /// <param name="text">Raw AI response text that may contain ORDER_CODE blocks.</param>
    /// <param name="collectedOrderCodes">
    /// Optional list to receive the trimmed order code value from each block.
    /// </param>
    /// <returns>The cleaned response text with all ORDER_CODE blocks removed.</returns>
    public static string StripOrderCodeBlocks(string text, List<string>? collectedOrderCodes = null)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var blockStart = text.IndexOf(OrderCodeStart, startIndex, StringComparison.OrdinalIgnoreCase);
            if (blockStart == -1)
                break;

            var blockEnd = text.IndexOf(BlockEnd, blockStart + OrderCodeStart.Length, StringComparison.OrdinalIgnoreCase);
            if (blockEnd == -1)
                break;

            var content = text
                .Substring(blockStart + OrderCodeStart.Length, blockEnd - (blockStart + OrderCodeStart.Length))
                .Trim();

            if (collectedOrderCodes is not null && content.Length > 0)
                collectedOrderCodes.Add(NormalizeDigits(content));

            var blockLength = (blockEnd + BlockEnd.Length) - blockStart;
            text = text.Remove(blockStart, blockLength);
            startIndex = blockStart;
        }

        return text.Trim();
    }

    /// <summary>
    /// Strips all <c>&lt;&lt;FEEDBACK … &gt;&gt;</c> blocks from <paramref name="text"/>.
    /// Extracted feedback JSON is returned via <paramref name="collectedFeedbackData"/>.
    /// </summary>
    /// <param name="text">Raw AI response text that may contain FEEDBACK blocks.</param>
    /// <param name="collectedFeedbackData">
    /// Output parameter to receive the trimmed feedback JSON from the block.
    /// </param>
    /// <returns>The cleaned response text with all FEEDBACK blocks removed.</returns>
    public static string StripFeedbackBlocks(string text, out string? collectedFeedbackData)
    {
        collectedFeedbackData = null;

        if (string.IsNullOrEmpty(text))
            return text;

        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var blockStart = text.IndexOf(FeedbackStart, startIndex, StringComparison.OrdinalIgnoreCase);
            if (blockStart == -1)
                break;

            var blockEnd = text.IndexOf(BlockEnd, blockStart + FeedbackStart.Length, StringComparison.OrdinalIgnoreCase);
            if (blockEnd == -1)
                break;

            var content = text
                .Substring(blockStart + FeedbackStart.Length, blockEnd - (blockStart + FeedbackStart.Length))
                .Trim();

            if (content.Length > 0)
                collectedFeedbackData = content;

            var blockLength = (blockEnd + BlockEnd.Length) - blockStart;
            text = text.Remove(blockStart, blockLength);
            startIndex = blockStart;
        }

        return text.Trim();
    }

    /// <summary>
    /// Converts Persian (۰–۹) and Arabic-Indic (٠–٩) digits to ASCII digits (0–9).
    /// </summary>
    public static string NormalizeDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            // Persian-Indic: U+06F0–U+06F9
            if (chars[i] >= '\u06F0' && chars[i] <= '\u06F9')
                chars[i] = (char)(chars[i] - '\u06F0' + '0');
            // Arabic-Indic: U+0660–U+0669
            else if (chars[i] >= '\u0660' && chars[i] <= '\u0669')
                chars[i] = (char)(chars[i] - '\u0660' + '0');
        }
        return new string(chars);
    }
}
