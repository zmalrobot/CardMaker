namespace CardMaker.AI.Abstractions;

public enum AiProgressStage
{
    Idle,
    Preparing,
    LoadingModel,
    Generating,
    Completed,
    Error,
}

public sealed record AiProgressUpdate(
    AiProgressStage Stage,
    string StatusMessage,
    double? Percentage = null,
    string? PartialToken = null);

public sealed record TextPromptRequest(
    string SystemPrompt,
    string UserPrompt,
    float Temperature = 0.7f,
    int MaxTokens = 512,
    string? GrammarBnf = null,
    int Seed = -1);

public sealed record TextPromptResult(
    string Text,
    long DurationMs,
    int TokensGenerated = 0);

