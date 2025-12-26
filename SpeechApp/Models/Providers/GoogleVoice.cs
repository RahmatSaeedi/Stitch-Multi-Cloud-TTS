using System.Text.Json.Serialization;

namespace SpeechApp.Models.Providers;

public class GoogleVoice
{
    public required string Name { get; set; }
    public List<string> LanguageCodes { get; set; } = new();
    public required string SsmlGender { get; set; }
    public int NaturalSampleRateHertz { get; set; }
}

public class GoogleVoicesResponse
{
    public List<GoogleVoice> Voices { get; set; } = new();
}

public class GoogleSynthesisRequest
{
    public required GoogleSynthesisInput Input { get; set; }
    public required GoogleVoiceSelection Voice { get; set; }
    public required GoogleAudioConfig AudioConfig { get; set; }
}

public class GoogleSynthesisInput
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ssml { get; set; }
}

public class GoogleVoiceSelection
{
    public required string LanguageCode { get; set; }
    public required string Name { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }
}

public class GoogleAudioConfig
{
    public required string AudioEncoding { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? SpeakingRate { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Pitch { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? VolumeGainDb { get; set; }
}

public class GoogleSynthesisResponse
{
    public required string AudioContent { get; set; }
}
