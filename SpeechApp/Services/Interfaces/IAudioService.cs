namespace SpeechApp.Services.Interfaces;

public interface IAudioService
{
    /// <summary>
    /// Merges multiple audio chunks into a single file
    /// </summary>
    Task<byte[]> MergeAudioChunksAsync(List<byte[]> chunks, string format = "mp3");

    /// <summary>
    /// Converts audio from one format to another
    /// </summary>
    Task<byte[]> ConvertAudioFormatAsync(byte[] audioData, string fromFormat, string toFormat);

    /// <summary>
    /// Adds ID3 tags to MP3 file
    /// </summary>
    Task<byte[]> AddId3TagsAsync(byte[] mp3Data, Dictionary<string, string> tags);

    /// <summary>
    /// Event raised when merge progress changes
    /// </summary>
    event Action<int, int>? OnMergeProgress;
}
