# PineSms.InstructionAnalyzer

This console application analyzes chat messages between users and the PineSms chatbot to provide recommendations for improving the bot's instruction file.

## Purpose

The analyzer:
1. Loads all chat messages from the database (`BotChatMessage` table)
2. Reads the current chatbot instruction file (`chtbot-instructions-main.md`)
3. Sends both to an LLM for analysis
4. Receives recommendations on how to improve the instruction file
5. Saves the recommendations to a text file

## Configuration

Edit `appsettings.json` before running:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../PineSms.BaleBot/bot.db"
  },
  "DatabaseProvider": "Sqlite",
  "AiAgent": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Model": "gpt-4.1",
    "Endpoint": "https://models.github.ai/inference"
  },
  "InstructionFilePath": "../PineSms.BaleBot/Chat/chtbot-instructions-main.md",
  "OutputFilePath": "instruction-analysis-recommendations.txt"
}
```

**Important:** You must set the `AiAgent:ApiKey` value before running the application.

## How to Run

```bash
cd PineSms.InstructionAnalyzer
dotnet run
```

The application will:
1. Connect to the database and load all chat messages
2. Read the instruction file
3. Send the data to the LLM for analysis
4. Save recommendations to the output file (default: `instruction-analysis-recommendations.txt`)

## Output

The application generates a text file containing:
- Analysis timestamp
- Number of messages analyzed
- LLM recommendations for improving the instruction file

## Notes

- This application does not modify any existing projects or databases
- It only reads data and creates a new output file
- The LLM analyzes actual user conversations to identify gaps in the current instructions
- Recommendations may include new topics, commands, or workflows to add to the instruction file
