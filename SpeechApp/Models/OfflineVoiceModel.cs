namespace SpeechApp.Models;

public class OfflineVoiceModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Language { get; set; }
    public required string LanguageCode { get; set; }
    public string? Gender { get; set; }
    public required string Quality { get; set; }
    public required long SizeBytes { get; set; }
    public required string Provider { get; set; }
    public bool IsDownloaded { get; set; }
    public DateTime? DownloadedDate { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets human-readable size (e.g., "45 MB")
    /// </summary>
    public string GetFormattedSize()
    {
        if (SizeBytes < 1024)
            return $"{SizeBytes} B";
        if (SizeBytes < 1024 * 1024)
            return $"{SizeBytes / 1024.0:F1} KB";
        if (SizeBytes < 1024 * 1024 * 1024)
            return $"{SizeBytes / (1024.0 * 1024.0):F1} MB";

        return $"{SizeBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
