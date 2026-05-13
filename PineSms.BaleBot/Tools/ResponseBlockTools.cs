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
    private const string ComplaintStart = "<<COMPLAINT";
    private const string SatisfactionStart = "<<SATISFACTION";
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
                collectedOrderCodes.Add(content);

            var blockLength = (blockEnd + BlockEnd.Length) - blockStart;
            text = text.Remove(blockStart, blockLength);
            startIndex = blockStart;
        }

        return text.Trim();
    }

    public static string StripComplaintBlocks(string text, out string? collectedComplaintData)
    {
        collectedComplaintData = null;

        if (string.IsNullOrEmpty(text))
            return text;

        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var blockStart = text.IndexOf(ComplaintStart, startIndex, StringComparison.OrdinalIgnoreCase);
            if (blockStart == -1)
                break;

            var blockEnd = text.IndexOf(BlockEnd, blockStart + ComplaintStart.Length, StringComparison.OrdinalIgnoreCase);
            if (blockEnd == -1)
                break;

            var content = text
                .Substring(blockStart + ComplaintStart.Length, blockEnd - (blockStart + ComplaintStart.Length))
                .Trim();

            if (content.Length > 0)
                collectedComplaintData = content;

            var blockLength = (blockEnd + BlockEnd.Length) - blockStart;
            text = text.Remove(blockStart, blockLength);
            startIndex = blockStart;
        }

        return text.Trim();
    }

    public static string StripSatisfactionBlocks(string text, out string? collectedSatisfactionData)
    {
        collectedSatisfactionData = null;

        if (string.IsNullOrEmpty(text))
            return text;

        var startIndex = 0;

        while (startIndex < text.Length)
        {
            var blockStart = text.IndexOf(SatisfactionStart, startIndex, StringComparison.OrdinalIgnoreCase);
            if (blockStart == -1)
                break;

            var blockEnd = text.IndexOf(BlockEnd, blockStart + SatisfactionStart.Length, StringComparison.OrdinalIgnoreCase);
            if (blockEnd == -1)
                break;

            var content = text
                .Substring(blockStart + SatisfactionStart.Length, blockEnd - (blockStart + SatisfactionStart.Length))
                .Trim();

            if (content.Length > 0)
                collectedSatisfactionData = content;

            var blockLength = (blockEnd + BlockEnd.Length) - blockStart;
            text = text.Remove(blockStart, blockLength);
            startIndex = blockStart;
        }

        return text.Trim();
    }
}
