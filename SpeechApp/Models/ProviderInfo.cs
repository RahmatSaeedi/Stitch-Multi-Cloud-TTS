namespace SpeechApp.Models;

public class ProviderInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public int MaxCharacterLimit { get; set; }
    public bool RequiresApiKey { get; set; } = true;
    public bool SupportsSSML { get; set; }
    public string? SetupGuideUrl { get; set; }
    public ProviderHealth Health { get; set; } = ProviderHealth.Unknown;
}

public enum ProviderHealth
{
    Unknown,
    Online,
    Degraded,
    Offline
}
