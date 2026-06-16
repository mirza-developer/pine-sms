# PineSms.InstructionAnalyzer

This console application analyzes chat messages between users and the PineSms chatbot, then automatically improves the bot's instruction file by applying high-impact enhancements identified by an LLM.

## Purpose

The analyzer:
1. Loads recent chat messages from the database (`BotChatMessage` table)
2. Reads the current chatbot instruction file (`chtbot-instructions-main.md`)
3. Sends both to an LLM which returns a list of scored improvement items
4. Displays all items with their scores (0–100)
5. Applies only items with a score **above 50** directly to the instruction file
6. Saves a timestamped backup of the original file to the history directory before any modification

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
  "HistoryDirectory": "../PineSms.BaleBot/Chat/history"
}
```

**Important:** You must set the `AiAgent:ApiKey` value before running the application.

## How to Run

```bash
cd PineSms.InstructionAnalyzer
dotnet run
```

The application will:
1. Connect to the database and load the 400 most recent chat messages
2. Read the instruction file
3. Send the data to the LLM and receive scored improvement items (JSON)
4. Print all items with their scores
5. Skip items with score ≤ 50 (no changes made if none qualify)
6. Save a timestamped backup of the current instruction file to the history directory
7. Append approved improvements directly to the instruction file
8. Save a detailed run log to the history directory

## Output

| File | Description |
|------|-------------|
| `{InstructionFilePath}` | Updated instruction file with approved improvements appended |
| `history/{name}_{timestamp}.md` | Original instruction file backed up before modification |
| `history/run-log_{timestamp}.txt` | Full log of all items found, scores, and decisions |

## Scoring

Each improvement item is scored from **0 to 100**:
- **> 50** — Applied to the instruction file
- **≤ 50** — Skipped (not applied)

If no items score above 50, the instruction file is left unchanged.

## Notes

- The instruction file is only modified when at least one item scores above 50
- Every run that modifies the file creates a new uniquely-named backup in the history directory
- The LLM analyzes actual user conversations to identify gaps in the current instructions
