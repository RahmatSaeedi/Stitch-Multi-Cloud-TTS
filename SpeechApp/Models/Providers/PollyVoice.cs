namespace SpeechApp.Models.Providers;

public class PollyVoice
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Gender { get; set; }
    public required string LanguageCode { get; set; }
    public required string LanguageName { get; set; }
    public List<string>? SupportedEngines { get; set; }
    public List<string>? AdditionalLanguageCodes { get; set; }
}

public class PollySynthesisRequest
{
    public required string Engine { get; set; }
    public required string LanguageCode { get; set; }
    public required string OutputFormat { get; set; }
    public required string Text { get; set; }
    public string? TextType { get; set; }
    public required string VoiceId { get; set; }
}

public class PollyDescribeVoicesResponse
{
    public List<PollyVoiceDescription>? Voices { get; set; }
}

public class PollyVoiceDescription
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Gender { get; set; }
    public required string LanguageCode { get; set; }
    public required string LanguageName { get; set; }
    public List<string>? SupportedEngines { get; set; }
    public List<string>? AdditionalLanguageCodes { get; set; }
}
