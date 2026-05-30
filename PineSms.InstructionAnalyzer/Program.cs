using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using PineSms.Core.Entities;
using PineSms.Persistence.Services;
using System.ClientModel;
using System.Text;

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
var outputFilePath = config["OutputFilePath"] ?? "instruction-analysis-recommendations.txt";

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

    messages = messages.OrderByDescending(p=>p.SentAt).Take(400).ToList();
}

Console.WriteLine($"Loaded {messages.Count} chat messages.");

if (messages.Count == 0)
{
    Console.WriteLine("WARNING: No chat messages found in the database.");
    Console.WriteLine("The analysis will continue but may not provide meaningful recommendations.");
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

promptBuilder.AppendLine("I am analyzing a chatbot system. Below you will find:");
promptBuilder.AppendLine("1. All real chat messages between users and the chatbot");
promptBuilder.AppendLine("2. The current instruction file that the chatbot follows");
promptBuilder.AppendLine();
promptBuilder.AppendLine("Your task is to analyze these messages and the instruction file, then provide recommendations for:");
promptBuilder.AppendLine("- New subjects or topics that should be added to the instruction file");
promptBuilder.AppendLine("- New commands or workflows that would improve user experience");
promptBuilder.AppendLine("- Common user questions or issues that are not currently covered");
promptBuilder.AppendLine("- Any patterns in user interactions that suggest missing functionality");
promptBuilder.AppendLine();
promptBuilder.AppendLine("Please provide specific, actionable recommendations.");
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
promptBuilder.AppendLine("Based on the above chat messages and instruction file, please provide your analysis and recommendations:");

var analysisPrompt = promptBuilder.ToString();

// Initialize AI client
Console.WriteLine("Initializing AI client...");
var chatClient = new ChatClient("gpt-4.1",
            new ApiKeyCredential(""),
            new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
            ;
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
    var recommendations = response.Value.Content[0].Text ?? "No response received.";

    // Save results to file
    Console.WriteLine("Saving recommendations to file...");
    var outputContent = new StringBuilder();
    outputContent.AppendLine("=== INSTRUCTION ANALYSIS RECOMMENDATIONS ===");
    outputContent.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    outputContent.AppendLine($"Analyzed {messages.Count} chat messages");
    outputContent.AppendLine();
    outputContent.AppendLine(recommendations);

    await File.WriteAllTextAsync(outputFilePath, outputContent.ToString());

    Console.WriteLine($"SUCCESS! Recommendations saved to: {outputFilePath}");
    Console.WriteLine();
    Console.WriteLine("=== RECOMMENDATIONS PREVIEW ===");
    Console.WriteLine(recommendations);
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: Failed to get recommendations from LLM: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    return;
}

Console.WriteLine();
Console.WriteLine("Analysis complete!");
Console.ReadKey();