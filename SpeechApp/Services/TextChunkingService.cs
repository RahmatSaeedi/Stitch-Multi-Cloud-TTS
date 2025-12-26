using SpeechApp.Services.Interfaces;
using System.Text.RegularExpressions;

namespace SpeechApp.Services;

public class TextChunkingService : ITextChunkingService
{
    private static readonly char[] SentenceEnders = new[] { '.', '!', '?' };
    private static readonly char[] ParagraphSeparators = new[] { '\n', '\r' };

    public List<string> ChunkText(string text, int maxChunkSize, bool respectSentences = true)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        if (text.Length <= maxChunkSize)
        {
            return new List<string> { text };
        }

        if (!respectSentences)
        {
            return SimpleChunk(text, maxChunkSize);
        }

        return SmartChunk(text, maxChunkSize);
    }

    public int EstimateChunkCount(string text, int maxChunkSize)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        if (text.Length <= maxChunkSize)
        {
            return 1;
        }

        // Rough estimate
        return (int)Math.Ceiling((double)text.Length / maxChunkSize);
    }

    private List<string> SimpleChunk(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        var currentIndex = 0;

        while (currentIndex < text.Length)
        {
            var remainingLength = text.Length - currentIndex;
            var chunkSize = Math.Min(maxChunkSize, remainingLength);

            chunks.Add(text.Substring(currentIndex, chunkSize));
            currentIndex += chunkSize;
        }

        return chunks;
    }

    private List<string> SmartChunk(string text, int maxChunkSize)
    {
        var chunks = new List<string>();

        // First split by paragraphs
        var paragraphs = text.Split(ParagraphSeparators, StringSplitOptions.None);

        var currentChunk = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                // Preserve empty lines
                if (currentChunk.Length > 0)
                {
                    currentChunk.AppendLine();
                }
                continue;
            }

            // If paragraph is too long, split by sentences
            if (paragraph.Length > maxChunkSize)
            {
                // Save current chunk if not empty
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().TrimEnd());
                    currentChunk.Clear();
                }

                // Split paragraph into sentences
                var sentences = SplitIntoSentences(paragraph);

                foreach (var sentence in sentences)
                {
                    // If single sentence is too long, force split
                    if (sentence.Length > maxChunkSize)
                    {
                        if (currentChunk.Length > 0)
                        {
                            chunks.Add(currentChunk.ToString().TrimEnd());
                            currentChunk.Clear();
                        }

                        // Force split long sentence
                        chunks.AddRange(SimpleChunk(sentence, maxChunkSize));
                    }
                    else if (currentChunk.Length + sentence.Length + 1 > maxChunkSize)
                    {
                        // Current chunk would be too large, save it and start new one
                        chunks.Add(currentChunk.ToString().TrimEnd());
                        currentChunk.Clear();
                        currentChunk.Append(sentence);
                        currentChunk.Append(' ');
                    }
                    else
                    {
                        // Add sentence to current chunk
                        currentChunk.Append(sentence);
                        currentChunk.Append(' ');
                    }
                }
            }
            else if (currentChunk.Length + paragraph.Length + 2 > maxChunkSize)
            {
                // Adding this paragraph would exceed limit, save current chunk
                chunks.Add(currentChunk.ToString().TrimEnd());
                currentChunk.Clear();
                currentChunk.AppendLine(paragraph);
            }
            else
            {
                // Add paragraph to current chunk
                currentChunk.AppendLine(paragraph);
            }
        }

        // Add final chunk if not empty
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().TrimEnd());
        }

        return chunks;
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();

        // Simple sentence detection using regex
        var pattern = @"(?<=[.!?])\s+(?=[A-Z])";
        var parts = Regex.Split(text, pattern);

        var currentSentence = new System.Text.StringBuilder();

        foreach (var part in parts)
        {
            currentSentence.Append(part);

            // Check if this ends with a sentence ender
            if (part.Length > 0 && SentenceEnders.Contains(part[part.Length - 1]))
            {
                sentences.Add(currentSentence.ToString().Trim());
                currentSentence.Clear();
            }
        }

        // Add any remaining text
        if (currentSentence.Length > 0)
        {
            sentences.Add(currentSentence.ToString().Trim());
        }

        // Fallback: if no sentences were detected, treat whole text as one sentence
        if (sentences.Count == 0)
        {
            sentences.Add(text.Trim());
        }

        return sentences;
    }
}
