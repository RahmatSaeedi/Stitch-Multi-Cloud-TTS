using Microsoft.JSInterop;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services;

public class AudioMergingService : IAudioService
{
    private readonly IJSRuntime _jsRuntime;

#pragma warning disable CS0067 // Event is declared but never used - reserved for future progress tracking
    public event Action<int, int>? OnMergeProgress;
#pragma warning restore CS0067

    public AudioMergingService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<byte[]> MergeAudioChunksAsync(List<byte[]> chunks, string format = "mp3")
    {
        if (chunks == null || chunks.Count == 0)
        {
            throw new ArgumentException("No audio chunks to merge", nameof(chunks));
        }

        if (chunks.Count == 1)
        {
            return chunks[0];
        }

        try
        {
            // Convert chunks to base64 for JS interop
            var base64Chunks = chunks.Select(chunk => Convert.ToBase64String(chunk)).ToList();

            // Call JavaScript audio merger
            var mergedBase64 = await _jsRuntime.InvokeAsync<string>("audioMerger.mergeAudioChunks", base64Chunks.ToArray());

            // Convert back to bytes
            return Convert.FromBase64String(mergedBase64);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Audio merging failed: {ex.Message}", ex);
        }
    }

    public Task<byte[]> ConvertAudioFormatAsync(byte[] audioData, string fromFormat, string toFormat)
    {
        // Format conversion would require additional libraries
        // For now, just return the original data
        // TODO: Implement format conversion using FFmpeg.wasm or similar
        return Task.FromResult(audioData);
    }

    public Task<byte[]> AddId3TagsAsync(byte[] mp3Data, Dictionary<string, string> tags)
    {
        // ID3 tag implementation would require a library
        // For now, just return the original data
        // TODO: Implement ID3 tagging
        return Task.FromResult(mp3Data);
    }

    /// <summary>
    /// Triggers a download of audio data in the browser
    /// </summary>
    public async Task DownloadAudioAsync(byte[] audioData, string filename, string mimeType = "audio/mpeg")
    {
        var base64 = Convert.ToBase64String(audioData);
        await _jsRuntime.InvokeVoidAsync("audioMerger.downloadAudio", base64, filename, mimeType);
    }
}
