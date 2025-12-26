namespace SpeechApp.Models;

public class VoiceConfig
{
    public required string VoiceId { get; set; }
    public required string ProviderId { get; set; }
    public double Speed { get; set; } = 1.0;
    public double Pitch { get; set; } = 0.0;
    public double Volume { get; set; } = 1.0;
    public string? OutputFormat { get; set; } = "mp3";
    public Dictionary<string, object>? ProviderSpecificOptions { get; set; }
}
