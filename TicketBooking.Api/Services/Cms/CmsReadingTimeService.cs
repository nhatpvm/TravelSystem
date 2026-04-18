// FILE #301: TicketBooking.Api/Services/Cms/CmsReadingTimeService.cs
using System.Text.RegularExpressions;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsReadingTimeService
{
    int CountWords(string? markdownOrText);
    int EstimateReadingMinutes(int wordCount);
    CmsReadingStats Calculate(string? markdownOrText);
}

public sealed class CmsReadingTimeService : ICmsReadingTimeService
{
    // SEO/content teams often use ~180-220 words/minute. Pick 200 for stable estimate.
    private const int DefaultWordsPerMinute = 200;

    // Strip common markdown/html noise before counting words.
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex MarkdownImageRegex = new(@"!\[(.*?)\]\((.*?)\)", RegexOptions.Compiled);
    private static readonly Regex MarkdownLinkRegex = new(@"\[(.*?)\]\((.*?)\)", RegexOptions.Compiled);
    private static readonly Regex MarkdownCodeFenceRegex = new(@"```[\s\S]*?```", RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]*)`", RegexOptions.Compiled);
    private static readonly Regex MarkdownHeadingQuoteListRegex = new(@"(^|\n)\s{0,3}(#{1,6}|\>|-|\*|\+|\d+\.)\s*", RegexOptions.Compiled);
    private static readonly Regex MarkdownTablePipeRegex = new(@"\|", RegexOptions.Compiled);
    private static readonly Regex EmphasisRegex = new(@"[*_~]+", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public int CountWords(string? markdownOrText)
    {
        if (string.IsNullOrWhiteSpace(markdownOrText))
            return 0;

        var plain = ToPlainText(markdownOrText);

        if (string.IsNullOrWhiteSpace(plain))
            return 0;

        // Count Unicode word-like tokens, including Vietnamese letters and numbers.
        var count = 0;
        var inWord = false;

        foreach (var ch in plain)
        {
            var isWordChar = char.IsLetterOrDigit(ch);

            if (isWordChar)
            {
                if (!inWord)
                {
                    count++;
                    inWord = true;
                }
            }
            else
            {
                inWord = false;
            }
        }

        return count;
    }

    public int EstimateReadingMinutes(int wordCount)
    {
        if (wordCount <= 0)
            return 0;

        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)DefaultWordsPerMinute));
    }

    public CmsReadingStats Calculate(string? markdownOrText)
    {
        var wordCount = CountWords(markdownOrText);
        return new CmsReadingStats
        {
            WordCount = wordCount,
            ReadingTimeMinutes = EstimateReadingMinutes(wordCount)
        };
    }

    private static string ToPlainText(string input)
    {
        var text = input;

        // Remove fenced code blocks fully.
        text = MarkdownCodeFenceRegex.Replace(text, " ");

        // Images: keep alt text if present.
        text = MarkdownImageRegex.Replace(text, m =>
        {
            var alt = m.Groups[1].Value;
            return string.IsNullOrWhiteSpace(alt) ? " " : $" {alt} ";
        });

        // Links: keep visible label.
        text = MarkdownLinkRegex.Replace(text, m =>
        {
            var label = m.Groups[1].Value;
            return string.IsNullOrWhiteSpace(label) ? " " : $" {label} ";
        });

        // Inline code: keep code content as readable text.
        text = InlineCodeRegex.Replace(text, m =>
        {
            var code = m.Groups[1].Value;
            return string.IsNullOrWhiteSpace(code) ? " " : $" {code} ";
        });

        // Drop markdown syntax tokens around headings/lists/quotes.
        text = MarkdownHeadingQuoteListRegex.Replace(text, "$1");

        // Tables and emphasis markers.
        text = MarkdownTablePipeRegex.Replace(text, " ");
        text = EmphasisRegex.Replace(text, "");

        // Strip HTML if editor content leaked into markdown.
        text = HtmlTagRegex.Replace(text, " ");

        // Decode a few common HTML entities to avoid undercount.
        text = text
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase);

        text = MultiWhitespaceRegex.Replace(text, " ").Trim();
        return text;
    }
}

public sealed class CmsReadingStats
{
    public int WordCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
}
