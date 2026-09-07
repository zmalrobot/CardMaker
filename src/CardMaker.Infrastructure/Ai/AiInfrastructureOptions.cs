namespace CardMaker.Infrastructure.Ai;

public sealed class AiInfrastructureOptions
{
    public string DataRoot { get; set; } = string.Empty;
    public string ModelsDirectory { get; set; } = string.Empty;
    public string SettingsFilePath { get; set; } = string.Empty;
}
