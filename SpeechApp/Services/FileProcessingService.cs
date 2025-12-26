using Microsoft.AspNetCore.Components.Forms;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services;

public class FileProcessingService : IFileProcessingService
{
    private readonly PdfProcessingService _pdfProcessor;
    private readonly TextFileProcessingService _textProcessor;

    public event Action<int>? OnProgress;

    public FileProcessingService(PdfProcessingService pdfProcessor, TextFileProcessingService textProcessor)
    {
        _pdfProcessor = pdfProcessor;
        _textProcessor = textProcessor;

        // Forward progress events
        _pdfProcessor.OnProgress += progress => OnProgress?.Invoke(progress);
        _textProcessor.OnProgress += progress => OnProgress?.Invoke(progress);
    }

    public async Task<FileProcessingResult> ProcessFileAsync(IBrowserFile file, bool enableOCR = false, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        if (extension == ".pdf")
        {
            return await _pdfProcessor.ProcessFileAsync(file, enableOCR, cancellationToken);
        }
        else
        {
            return await _textProcessor.ProcessFileAsync(file, enableOCR, cancellationToken);
        }
    }

    public bool ValidateFile(IBrowserFile file, out string? errorMessage)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        if (extension == ".pdf")
        {
            return _pdfProcessor.ValidateFile(file, out errorMessage);
        }
        else
        {
            return _textProcessor.ValidateFile(file, out errorMessage);
        }
    }

    public async Task<bool> IsScannedPdfAsync(IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        if (extension == ".pdf")
        {
            return await _pdfProcessor.IsScannedPdfAsync(file);
        }

        return false;
    }
}
