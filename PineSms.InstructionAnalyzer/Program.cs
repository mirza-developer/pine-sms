using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using PineSms.Core.Entities;
using PineSms.Persistence.Services;
using System.ClientModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("=== PineSms Instruction Analyzer ===");
Console.WriteLine();

// Load configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var connectionString = config["ConnectionStrings:DefaultConnection"];
var databaseProvider = config["DatabaseProvider"] ?? "Sqlite";
var apiKey = config["AiAgent:ApiKey"];
var model = config["AiAgent:Model"] ?? "gpt-4.1";
var endpoint = config["AiAgent:Endpoint"] ?? "https://models.github.ai/inference";
var instructionFilePath = config["InstructionFilePath"] ?? "../PineSms.BaleBot/Chat/chtbot-instructions-main.md";
var historyDirectory = config["HistoryDirectory"] ?? Path.Combine(Path.GetDirectoryName(instructionFilePath)!, "history");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("ERROR: AiAgent:ApiKey is not configured in appsettings.json");
    Console.WriteLine("Please set your AI API key and try again.");
    return;
}

// Setup database context
Console.WriteLine($"Connecting to database ({databaseProvider})...");
var optionsBuilder = new DbContextOptionsBuilder<PineSmsDbContext>();

if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    optionsBuilder.UseSqlServer(connectionString);
}
else
{
    optionsBuilder.UseSqlite(connectionString);
}

// Load all chat messages from database
Console.WriteLine("Loading chat messages from database...");
List<BotChatMessage> messages;
using (var dbContext = new PineSmsDbContext(optionsBuilder.Options))
{
    messages = await dbContext.BotChatMessage
        .OrderBy(m => m.SentAt)
        .ToListAsync();

    messages = messages.OrderByDescending(p => p.SentAt).Take(400).ToList();
}

Console.WriteLine($"Loaded {messages.Count} chat messages.");

if (messages.Count == 0)
{
    Console.WriteLine("WARNING: No chat messages found in the database.");
    Console.WriteLine("The analysis will continue but may not provide meaningful results.");
}

// Read instruction file
Console.WriteLine($"Reading instruction file: {instructionFilePath}");
string instructionContent;
try
{
    instructionContent = await File.ReadAllTextAsync(instructionFilePath);
    Console.WriteLine($"Instruction file loaded ({instructionContent.Length} characters).");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to read instruction file: {ex.Message}");
    return;
}

// Prepare the analysis prompt
Console.WriteLine("Preparing analysis prompt...");
var promptBuilder = new StringBuilder();

promptBuilder.AppendLine("You are analyzing a chatbot system. Below you will find:");
promptBuilder.AppendLine("1. Recent real chat messages between users and the chatbot");
promptBuilder.AppendLine("2. The current instruction file that the chatbot follows");
promptBuilder.AppendLine();
promptBuilder.AppendLine("Your task is to identify specific items that would make the instruction file better.");
promptBuilder.AppendLine("For each item provide:");
promptBuilder.AppendLine("- A short title");
promptBuilder.AppendLine("- A description of what gap or issue it addresses");
promptBuilder.AppendLine("- A score from 0 to 100 indicating how much this item improves the instruction file");
promptBuilder.AppendLine("  (100 = critical improvement, 50 = moderate benefit, 0 = negligible)");
promptBuilder.AppendLine("- The exact markdown content to append to the instruction file");
promptBuilder.AppendLine();
promptBuilder.AppendLine("You MUST respond ONLY with a valid JSON array. Do not include any text outside the JSON.");
promptBuilder.AppendLine("Use this exact format:");
promptBuilder.AppendLine("[");
promptBuilder.AppendLine("  {");
promptBuilder.AppendLine("    \"title\": \"Short title of the improvement\",");
promptBuilder.AppendLine("    \"description\": \"Why this improves the instruction file\",");
promptBuilder.AppendLine("    \"score\": 85,");
promptBuilder.AppendLine("    \"content\": \"The exact markdown section to add to the instruction file\"");
promptBuilder.AppendLine("  }");
promptBuilder.AppendLine("]");
promptBuilder.AppendLine();
promptBuilder.AppendLine("=== CHAT MESSAGES ===");
promptBuilder.AppendLine();

foreach (var msg in messages)
{
    var sender = msg.IsFromBot ? "BOT" : "USER";
    promptBuilder.AppendLine($"[{msg.SentAt:yyyy-MM-dd HH:mm:ss}] {sender} ({msg.BaleUsername}): {msg.MessageText}");
}

promptBuilder.AppendLine();
promptBuilder.AppendLine("=== CURRENT INSTRUCTION FILE CONTENT ===");
promptBuilder.AppendLine();
promptBuilder.AppendLine(instructionContent);
promptBuilder.AppendLine();
promptBuilder.AppendLine("=== END OF INPUT ===");
promptBuilder.AppendLine();
promptBuilder.AppendLine("Now return the JSON array of improvement items:");

var analysisPrompt = promptBuilder.ToString();

// Initialize AI client
Console.WriteLine("Initializing AI client...");
var chatClient = new ChatClient(model,
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

// Send request to LLM
Console.WriteLine("Sending analysis request to LLM (this may take a while)...");
Console.WriteLine();

try
{
    var chatMessages = new List<ChatMessage>
    {
        new UserChatMessage(analysisPrompt)
    };

    var response = await chatClient.CompleteChatAsync(chatMessages);
    var rawResponse = response.Value.Content[0].Text ?? "[]";

    // Parse improvement items from JSON response
    Console.WriteLine("Parsing improvement items...");
    List<ImprovementItem> allItems;
    try
    {
        // Strip markdown code fences if present
        var jsonText = rawResponse.Trim();
        if (jsonText.StartsWith("```"))
        {
            var start = jsonText.IndexOf('[');
            var end = jsonText.LastIndexOf(']');
            if (start >= 0 && end > start)
                jsonText = jsonText[start..(end + 1)];
        }

        allItems = JsonSerializer.Deserialize<List<ImprovementItem>>(jsonText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Could not parse JSON response: {ex.Message}");
        Console.WriteLine("Raw response:");
        Console.WriteLine(rawResponse);
        allItems = [];
    }

    Console.WriteLine($"Total improvement items identified: {allItems.Count}");
    Console.WriteLine();

    // Display all items with scores
    Console.WriteLine("=== ALL IMPROVEMENT ITEMS ===");
    foreach (var item in allItems.OrderByDescending(i => i.Score))
    {
        var status = item.Score > 50 ? "[WILL APPLY]" : "[SKIPPED   ]";
        Console.WriteLine($"  {status} Score: {item.Score,3}/100 | {item.Title}");
        Console.WriteLine($"             {item.Description}");
        Console.WriteLine();
    }

    // Filter items with score > 50
    var itemsToApply = allItems.Where(i => i.Score > 50).OrderByDescending(i => i.Score).ToList();

    if (itemsToApply.Count == 0)
    {
        Console.WriteLine("No improvement items scored above 50. The instruction file will NOT be modified.");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine($"Applying {itemsToApply.Count} improvement item(s) with score > 50...");
        Console.WriteLine();

        // Save history of the current instruction file before modifying
        Directory.CreateDirectory(historyDirectory);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var instructionFileName = Path.GetFileNameWithoutExtension(instructionFilePath);
        var instructionFileExt = Path.GetExtension(instructionFilePath);
        var historyFilePath = Path.Combine(historyDirectory, $"{instructionFileName}_{timestamp}{instructionFileExt}");

        await File.WriteAllTextAsync(historyFilePath, instructionContent);
        Console.WriteLine($"History saved: {historyFilePath}");

        // Build updated instruction content
        var updatedContent = new StringBuilder(instructionContent);
        updatedContent.AppendLine();
        updatedContent.AppendLine("---");
        updatedContent.AppendLine();
        updatedContent.AppendLine($"<!-- Auto-generated improvements applied on {DateTime.Now:yyyy-MM-dd HH:mm:ss} -->");
        updatedContent.AppendLine();

        foreach (var item in itemsToApply)
        {
            Console.WriteLine($"  Applying (score {item.Score}): {item.Title}");
            updatedContent.AppendLine(item.Content);
            updatedContent.AppendLine();
        }

        // Overwrite the instruction file with improved content
        await File.WriteAllTextAsync(instructionFilePath, updatedContent.ToString());
        Console.WriteLine();
        Console.WriteLine($"SUCCESS! Instruction file updated: {instructionFilePath}");
        Console.WriteLine($"Applied {itemsToApply.Count} improvement(s).");

        // Also save a run log for reference
        var runLogPath = Path.Combine(historyDirectory, $"run-log_{timestamp}.txt");
        var logContent = new StringBuilder();
        logContent.AppendLine("=== INSTRUCTION ANALYZER RUN LOG ===");
        logContent.AppendLine($"Run time   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        logContent.AppendLine($"Messages   : {messages.Count} analyzed");
        logContent.AppendLine($"Items found: {allItems.Count} total, {itemsToApply.Count} applied");
        logContent.AppendLine();
        logContent.AppendLine("=== ITEMS APPLIED (score > 50) ===");
        foreach (var item in itemsToApply)
        {
            logContent.AppendLine($"  [{item.Score}/100] {item.Title}");
            logContent.AppendLine($"  Reason: {item.Description}");
            logContent.AppendLine();
        }
        logContent.AppendLine("=== ITEMS SKIPPED (score <= 50) ===");
        foreach (var item in allItems.Where(i => i.Score <= 50).OrderByDescending(i => i.Score))
        {
            logContent.AppendLine($"  [{item.Score}/100] {item.Title}");
            logContent.AppendLine($"  Reason: {item.Description}");
            logContent.AppendLine();
        }
        await File.WriteAllTextAsync(runLogPath, logContent.ToString());
        Console.WriteLine($"Run log saved: {runLogPath}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to get analysis from LLM: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return;
}

Console.WriteLine();
Console.WriteLine("Analysis complete!");
Console.ReadKey();

/// <summary>Represents a single improvement item returned by the AI.</summary>
internal sealed class ImprovementItem
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}