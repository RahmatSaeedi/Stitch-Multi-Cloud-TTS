using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SpeechApp.Services.Interfaces;
using System.Text.Json;

namespace SpeechApp.Services;

public class PdfProcessingService : IFileProcessingService
{
    private readonly IJSRuntime _jsRuntime;
    private const long MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB
    private static readonly string[] SUPPORTED_EXTENSIONS = { ".pdf" };

    public event Action<int>? OnProgress;

    public PdfProcessingService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<FileProcessingResult> ProcessFileAsync(IBrowserFile file, bool enableOCR = false, CancellationToken cancellationToken = default)
    {
        try
        {
            OnProgress?.Invoke(5);

            // Validate file
            if (!ValidateFile(file, out var errorMessage))
            {
                return new FileProcessingResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            OnProgress?.Invoke(10);

            // Read file bytes
            byte[] fileBytes;
            try
            {
                using var stream = file.OpenReadStream(MAX_FILE_SIZE, cancellationToken);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }
            catch (Exception ex) when (ex.Message.Contains("_blazorFilesById") ||
                                       ex.Message.Contains("exceeded the maximum") ||
                                       ex.Message.Contains("size limit"))
            {
                return new FileProcessingResult
                {
                    Success = false,
                    ErrorMessage = $"Unable to read file. This may be due to file size exceeding {MAX_FILE_SIZE / (1024 * 1024)} MB or browser limitations. Please try with a smaller file or try uploading again."
                };
            }

            OnProgress?.Invoke(20);

            // Convert to base64 for JavaScript
            var base64Data = Convert.ToBase64String(fileBytes);

            OnProgress?.Invoke(25);

            // Create progress callback for JavaScript
            var progressCallback = DotNetObjectReference.Create(new ProgressCallback(progress =>
            {
                var adjustedProgress = 25 + (int)(progress * 0.65); // Scale 0-100 to 25-90
                OnProgress?.Invoke(adjustedProgress);
            }));

            // Extract text using PDF.js
            var result = await _jsRuntime.InvokeAsync<PdfExtractionResult>(
                "pdfHelper.extractText",
                cancellationToken,
                base64Data,
                progressCallback
            );

            progressCallback.Dispose();

            OnProgress?.Invoke(90);

            if (!result.Success)
            {
                return new FileProcessingResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage ?? "Failed to extract text from PDF"
                };
            }

            // Get metadata
            var metadata = await GetPdfMetadataAsync(base64Data, cancellationToken);

            OnProgress?.Invoke(95);

            // Convert JavaScript chapters to C# chapters
            var chapters = result.Chapters?.Select(c => new Chapter
            {
                Number = c.Number,
                Title = c.Title ?? string.Empty,
                StartPosition = c.StartPosition,
                EndPosition = c.EndPosition,
                Preview = c.Preview
            }).ToList();

            OnProgress?.Invoke(100);

            return new FileProcessingResult
            {
                Success = true,
                ExtractedText = result.Text,
                IsScanned = result.IsScanned,
                Chapters = chapters,
                Metadata = new FileMetadata
                {
                    Title = metadata?.Title ?? Path.GetFileNameWithoutExtension(file.Name),
                    Author = metadata?.Author,
                    FileSizeBytes = file.Size,
                    FileType = ".pdf",
                    ProcessedDate = DateTime.UtcNow,
                    PageCount = result.Metadata?.PageCount ?? 0
                }
            };
        }
        catch (Exception ex)
        {
            return new FileProcessingResult
            {
                Success = false,
                ErrorMessage = $"Failed to process PDF: {ex.Message}"
            };
        }
    }

    public bool ValidateFile(IBrowserFile file, out string? errorMessage)
    {
        errorMessage = null;

        // Check file size
        if (file.Size > MAX_FILE_SIZE)
        {
            errorMessage = $"PDF file size exceeds maximum allowed size of {MAX_FILE_SIZE / 1024 / 1024} MB";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!SUPPORTED_EXTENSIONS.Contains(extension))
        {
            errorMessage = "File must be a PDF";
            return false;
        }

        return true;
    }

    public async Task<bool> IsScannedPdfAsync(IBrowserFile file)
    {
        try
        {
            using var stream = file.OpenReadStream(MAX_FILE_SIZE);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            var base64Data = Convert.ToBase64String(fileBytes);

            var result = await _jsRuntime.InvokeAsync<PdfExtractionResult>(
                "pdfHelper.extractText",
                base64Data,
                null
            );

            return result.IsScanned;
        }
        catch
        {
            return false;
        }
    }

    private async Task<PdfMetadata?> GetPdfMetadataAsync(string base64Data, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<PdfMetadata?>(
                "pdfHelper.getMetadata",
                cancellationToken,
                base64Data
            );
        }
        catch
        {
            return null;
        }
    }

    // Helper class for progress callback
    public class ProgressCallback
    {
        private readonly Action<int> _onProgress;

        public ProgressCallback(Action<int> onProgress)
        {
            _onProgress = onProgress;
        }

        [JSInvokable]
        public void Invoke(int progress)
        {
            _onProgress?.Invoke(progress);
        }
    }

    // JavaScript interop models
    private class PdfExtractionResult
    {
        public bool Success { get; set; }
        public string? Text { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsScanned { get; set; }
        public List<JsChapter>? Chapters { get; set; }
        public PdfExtractionMetadata? Metadata { get; set; }
    }

    private class JsChapter
    {
        public int Number { get; set; }
        public string? Title { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public string? Preview { get; set; }
    }

    private class PdfExtractionMetadata
    {
        public int PageCount { get; set; }
        public int ImageCount { get; set; }
        public int TextItemCount { get; set; }
    }

    private class PdfMetadata
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Subject { get; set; }
        public string? Creator { get; set; }
        public string? Producer { get; set; }
        public string? CreationDate { get; set; }
    }
}
