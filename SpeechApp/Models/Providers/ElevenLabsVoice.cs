using System.Text.Json.Serialization;

namespace SpeechApp.Models.Providers;

public class ElevenLabsVoice
{
    [JsonPropertyName("voice_id")]
    public required string VoiceId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("category")]
    public required string Category { get; set; }

    [JsonPropertyName("labels")]
    public Dictionary<string, string> Labels { get; set; } = new();

    [JsonPropertyName("settings")]
    public Dictionary<string, object>? Settings { get; set; }
}

public class ElevenLabsVoicesResponse
{
    [JsonPropertyName("voices")]
    public List<ElevenLabsVoice> Voices { get; set; } = new();
}

public class ElevenLabsSynthesisRequest
{
    [JsonPropertyName("text")]
    public required string Text { get; set; }

    [JsonPropertyName("model_id")]
    public string? ModelId { get; set; } = "eleven_multilingual_v2";

    [JsonPropertyName("voice_settings")]
    public ElevenLabsVoiceSettings? VoiceSettings { get; set; }
}

public class ElevenLabsVoiceSettings
{
    [JsonPropertyName("stability")]
    public double Stability { get; set; } = 0.5;

    [JsonPropertyName("similarity_boost")]
    public double SimilarityBoost { get; set; } = 0.75;

    [JsonPropertyName("style")]
    public double? Style { get; set; }

    [JsonPropertyName("use_speaker_boost")]
    public bool? UseSpeakerBoost { get; set; } = true;
}
