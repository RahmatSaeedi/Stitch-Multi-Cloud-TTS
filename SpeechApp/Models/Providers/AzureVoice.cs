namespace SpeechApp.Models.Providers;

public class AzureVoice
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string LocalName { get; set; }
    public required string ShortName { get; set; }
    public required string Gender { get; set; }
    public required string Locale { get; set; }
    public List<string>? StyleList { get; set; }
    public string? VoiceType { get; set; }
}

public class AzureVoicesResponse
{
    public List<AzureVoice>? Voices { get; set; }
}
