namespace SpeechApp.Services;

public static class ErrorMessageHelper
{
    public static string GetUserFriendlyMessage(Exception ex, string? context = null)
    {
        var message = ex.Message.ToLower();

        // Network errors
        if (ex is HttpRequestException)
        {
            if (message.Contains("name or service not known") || message.Contains("no such host"))
            {
                return "Unable to connect to the service. Please check your internet connection.";
            }
            if (message.Contains("timeout") || message.Contains("timed out"))
            {
                return "The request timed out. Please try again.";
            }
            return "Network error occurred. Please check your connection and try again.";
        }

        // Rate limiting
        if (message.Contains("429") || message.Contains("too many requests"))
        {
            return "Rate limit exceeded. Please wait a moment before trying again.";
        }

        // Authentication errors
        if (message.Contains("401") || message.Contains("unauthorized") || message.Contains("invalid api key"))
        {
            return "Invalid API key. Please check your API key in Settings.";
        }

        if (message.Contains("403") || message.Contains("forbidden"))
        {
            return "Access denied. Please verify your API key has the necessary permissions.";
        }

        // Service errors
        if (message.Contains("500") || message.Contains("internal server error"))
        {
            return "The service is experiencing issues. Please try again later.";
        }

        if (message.Contains("503") || message.Contains("service unavailable"))
        {
            return "The service is temporarily unavailable. Please try again in a few moments.";
        }

        // Quota/billing errors
        if (message.Contains("quota") || message.Contains("insufficient funds") || message.Contains("billing"))
        {
            return "API quota exceeded or billing issue. Please check your provider account.";
        }

        // File errors
        if (message.Contains("file") && message.Contains("size"))
        {
            return "File is too large. Please use a smaller file.";
        }

        if (message.Contains("unsupported") && message.Contains("format"))
        {
            return "Unsupported file format. Please use PDF or text files.";
        }

        // Character limit errors
        if (message.Contains("character") && (message.Contains("limit") || message.Contains("maximum")))
        {
            return "Text exceeds the maximum character limit for this provider. Try splitting it into smaller sections.";
        }

        // Generic context-based messages
        if (!string.IsNullOrEmpty(context))
        {
            return $"{context} failed: {GetShortErrorMessage(ex.Message)}";
        }

        return GetShortErrorMessage(ex.Message);
    }

    public static string GetShortErrorMessage(string fullMessage)
    {
        // Try to extract the most relevant part of the error message
        if (fullMessage.Length > 150)
        {
            return fullMessage.Substring(0, 147) + "...";
        }
        return fullMessage;
    }

    public static (string Title, string Details) GetErrorDetails(Exception ex)
    {
        var title = ex switch
        {
            HttpRequestException => "Network Error",
            TaskCanceledException => "Request Timeout",
            TimeoutException => "Request Timeout",
            UnauthorizedAccessException => "Access Denied",
            ArgumentException => "Invalid Input",
            _ => "Error"
        };

        var details = GetUserFriendlyMessage(ex);
        return (title, details);
    }
}
