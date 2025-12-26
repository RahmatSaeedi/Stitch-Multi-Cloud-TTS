namespace SpeechApp.Models;

public class SynthesisResult
{
    public bool Success { get; set; }
    public byte[]? AudioData { get; set; }
    public string? ErrorMessage { get; set; }
    public int CharactersProcessed { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan Duration { get; set; }
}
