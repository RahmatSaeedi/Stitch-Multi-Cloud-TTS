using Microsoft.AspNetCore.Components.Forms;
using SpeechApp.Services.Interfaces;
using System.Text;

namespace SpeechApp.Services;

public class TextFileProcessingService : IFileProcessingService
{
    private const long MAX_FILE_SIZE = 50 * 1024 * 1024; // 50 MB
    private static readonly string[] SUPPORTED_EXTENSIONS = { ".txt", ".md", ".log", ".csv", ".json", ".xml", ".html", ".htm" };

    public event Action<int>? OnProgress;

    public async Task<FileProcessingResult> ProcessFileAsync(IBrowserFile file, bool enableOCR = false, CancellationToken cancellationToken = default)
    {
        try
        {
            OnProgress?.Invoke(10);

            // Validate file
            if (!ValidateFile(file, out var errorMessage))
            {
                return new FileProcessingResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            OnProgress?.Invoke(25);

            // Read file bytes
            using var stream = file.OpenReadStream(MAX_FILE_SIZE, cancellationToken);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var fileBytes = memoryStream.ToArray();

            OnProgress?.Invoke(50);

            // Detect encoding
            var encoding = DetectEncoding(fileBytes);
            var detectedEncodingName = encoding.EncodingName;

            OnProgress?.Invoke(60);

            // Convert to string and normalize
            var text = encoding.GetString(fileBytes);

            // Remove BOM if present
            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }

            OnProgress?.Invoke(75);

            // Normalize line endings to \n
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            OnProgress?.Invoke(90);

            // Detect chapters (for markdown files)
            var chapters = file.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                ? DetectMarkdownChapters(text)
                : null;

            OnProgress?.Invoke(100);

            return new FileProcessingResult
            {
                Success = true,
                ExtractedText = text,
                Metadata = new FileMetadata
                {
                    Title = Path.GetFileNameWithoutExtension(file.Name),
                    FileSizeBytes = file.Size,
                    FileType = Path.GetExtension(file.Name),
                    ProcessedDate = DateTime.UtcNow,
                    Encoding = detectedEncodingName,
                    PageCount = 1
                },
                Chapters = chapters,
                IsScanned = false
            };
        }
        catch (Exception ex)
        {
            return new FileProcessingResult
            {
                Success = false,
                ErrorMessage = $"Failed to process file: {ex.Message}"
            };
        }
    }

    public bool ValidateFile(IBrowserFile file, out string? errorMessage)
    {
        errorMessage = null;

        // Check file size
        if (file.Size > MAX_FILE_SIZE)
        {
            errorMessage = $"File size exceeds maximum allowed size of {MAX_FILE_SIZE / 1024 / 1024} MB";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!SUPPORTED_EXTENSIONS.Contains(extension))
        {
            errorMessage = $"Unsupported file type. Supported types: {string.Join(", ", SUPPORTED_EXTENSIONS)}";
            return false;
        }

        return true;
    }

    public Task<bool> IsScannedPdfAsync(IBrowserFile file)
    {
        // Not a PDF processor, so always return false
        return Task.FromResult(false);
    }

    private Encoding DetectEncoding(byte[] fileBytes)
    {
        // Check for BOM
        if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
        {
            return Encoding.UTF8; // UTF-8 BOM
        }

        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
        {
            return Encoding.Unicode; // UTF-16 LE
        }

        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode; // UTF-16 BE
        }

        if (fileBytes.Length >= 4 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE && fileBytes[2] == 0x00 && fileBytes[3] == 0x00)
        {
            return Encoding.UTF32; // UTF-32 LE
        }

        // No BOM detected, try to detect encoding by content
        // Simple heuristic: if we can decode as UTF-8 without errors, use UTF-8, otherwise use ASCII/Latin1
        try
        {
            var decoder = Encoding.UTF8.GetDecoder();
            decoder.GetCharCount(fileBytes, 0, fileBytes.Length, true);
            return Encoding.UTF8;
        }
        catch
        {
            return Encoding.ASCII;
        }
    }

    private List<Chapter>? DetectMarkdownChapters(string text)
    {
        var chapters = new List<Chapter>();
        var lines = text.Split('\n');
        var chapterNumber = 0;
        var currentPosition = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Detect markdown headers (# Header or ## Header)
            if (line.StartsWith("# ") || line.StartsWith("## "))
            {
                // Close previous chapter if exists
                if (chapters.Count > 0)
                {
                    chapters[^1].EndPosition = currentPosition;
                }

                chapterNumber++;
                var title = line.TrimStart('#').Trim();
                var preview = i + 1 < lines.Length
                    ? string.Join(" ", lines.Skip(i + 1).Take(3).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)))
                    : string.Empty;

                chapters.Add(new Chapter
                {
                    Number = chapterNumber,
                    Title = title,
                    StartPosition = currentPosition,
                    EndPosition = text.Length, // Will be updated when next chapter is found
                    Preview = preview.Length > 100 ? preview.Substring(0, 100) + "..." : preview
                });
            }

            currentPosition += lines[i].Length + 1; // +1 for the newline
        }

        return chapters.Count > 0 ? chapters : null;
    }
}
