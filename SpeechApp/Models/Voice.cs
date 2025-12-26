namespace SpeechApp.Models;

public class Voice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public string? Quality { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}
