using Microsoft.AspNetCore.Components.Forms;

namespace SpeechApp.Services.Interfaces;

public interface IFileProcessingService
{
    /// <summary>
    /// Processes an uploaded file and extracts text with metadata
    /// </summary>
    Task<FileProcessingResult> ProcessFileAsync(IBrowserFile file, bool enableOCR = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates file size and type
    /// </summary>
    bool ValidateFile(IBrowserFile file, out string? errorMessage);

    /// <summary>
    /// Detects if a PDF is scanned (image-based)
    /// </summary>
    Task<bool> IsScannedPdfAsync(IBrowserFile file);

    /// <summary>
    /// Event raised when processing progress changes
    /// </summary>
    event Action<int>? OnProgress;
}

public class FileProcessingResult
{
    public bool Success { get; set; }
    public string? ExtractedText { get; set; }
    public string? ErrorMessage { get; set; }
    public FileMetadata? Metadata { get; set; }
    public List<Chapter>? Chapters { get; set; }
    public bool IsScanned { get; set; }
}

public class FileMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string? FileType { get; set; }
    public DateTime ProcessedDate { get; set; }
    public string? Encoding { get; set; }
}

public class Chapter
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
    public string? Preview { get; set; }
}
