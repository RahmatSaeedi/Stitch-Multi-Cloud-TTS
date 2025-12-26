using System.Text.Json.Serialization;

namespace SpeechApp.Models.Providers;

public class DeepgramVoice
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("canonical_name")]
    public required string CanonicalName { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("language_code")]
    public string? LanguageCode { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

public class DeepgramVoicesResponse
{
    [JsonPropertyName("voices")]
    public List<DeepgramVoice> Voices { get; set; } = new();
}

public class DeepgramSynthesisRequest
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }
}
