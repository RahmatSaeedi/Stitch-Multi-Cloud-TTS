namespace SpeechApp.Services.Interfaces;

public interface ITextChunkingService
{
    /// <summary>
    /// Splits text into chunks that respect provider character limits
    /// </summary>
    List<string> ChunkText(string text, int maxChunkSize, bool respectSentences = true);

    /// <summary>
    /// Estimates the number of chunks for a given text and provider
    /// </summary>
    int EstimateChunkCount(string text, int maxChunkSize);
}
