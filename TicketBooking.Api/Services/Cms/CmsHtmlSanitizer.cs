// FILE #305: TicketBooking.Api/Services/Cms/CmsHtmlSanitizer.cs
using System.Text.RegularExpressions;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsHtmlSanitizer
{
    string Sanitize(string? html);
    bool IsSafeUrl(string? url);
}

public sealed class CmsHtmlSanitizer : ICmsHtmlSanitizer
{
    // NOTE:
    // This is a conservative built-in sanitizer without external packages.
    // It removes obviously dangerous tags/attributes/protocols.
    // Later, if you want stronger sanitization, you can replace implementation
    // with a dedicated library (for example Ganss.Xss) without changing controllers/services.

    private static readonly Regex HtmlCommentRegex =
        new(@"<!--(.*?)-->", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex DangerousBlockTagsRegex =
        new(
            @"<\s*(script|style|iframe|object|embed|form|input|button|textarea|select|option|link|meta|base|frame|frameset|applet|svg|math)[^>]*>.*?<\s*/\s*\1\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex DangerousSingleTagsRegex =
        new(
            @"<\s*(script|style|iframe|object|embed|input|button|textarea|select|option|link|meta|base|frame|applet|svg|math)\b[^>]*\/?\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EventHandlerAttrDoubleQuoteRegex =
        new(@"\s(on[a-z0-9_-]+)\s*=\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EventHandlerAttrSingleQuoteRegex =
        new(@"\s(on[a-z0-9_-]+)\s*=\s*'[^']*'", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex EventHandlerAttrBareRegex =
        new(@"\s(on[a-z0-9_-]+)\s*=\s*[^\s>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StyleAttrDoubleQuoteRegex =
        new(@"\sstyle\s*=\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StyleAttrSingleQuoteRegex =
        new(@"\sstyle\s*=\s*'[^']*'", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StyleAttrBareRegex =
        new(@"\sstyle\s*=\s*[^\s>]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DangerousHrefSrcDoubleQuoteRegex =
        new(@"\s(href|src|xlink:href|formaction)\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DangerousHrefSrcSingleQuoteRegex =
        new(@"\s(href|src|xlink:href|formaction)\s*=\s*'([^']*)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DangerousHrefSrcBareRegex =
        new(@"\s(href|src|xlink:href|formaction)\s*=\s*([^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex XmlNsRegex =
        new(@"\sxmlns(:[a-z0-9_-]+)?\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SrcDocRegex =
        new(@"\ssrcdoc\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExcessWhitespaceRegex =
        new(@">\s+<", RegexOptions.Compiled);

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var result = html.Trim();

        // Remove comments first
        result = HtmlCommentRegex.Replace(result, string.Empty);

        // Remove dangerous tags / blocks
        result = DangerousBlockTagsRegex.Replace(result, string.Empty);
        result = DangerousSingleTagsRegex.Replace(result, string.Empty);

        // Remove inline event handlers: onclick, onerror, onload...
        result = EventHandlerAttrDoubleQuoteRegex.Replace(result, string.Empty);
        result = EventHandlerAttrSingleQuoteRegex.Replace(result, string.Empty);
        result = EventHandlerAttrBareRegex.Replace(result, string.Empty);

        // Remove inline style to reduce css-based abuse / tracking / weird render
        result = StyleAttrDoubleQuoteRegex.Replace(result, string.Empty);
        result = StyleAttrSingleQuoteRegex.Replace(result, string.Empty);
        result = StyleAttrBareRegex.Replace(result, string.Empty);

        // Remove xmlns / srcdoc
        result = XmlNsRegex.Replace(result, string.Empty);
        result = SrcDocRegex.Replace(result, string.Empty);

        // Clean dangerous protocols in href/src/formaction...
        result = DangerousHrefSrcDoubleQuoteRegex.Replace(result, SanitizeUrlMatchDoubleQuoted);
        result = DangerousHrefSrcSingleQuoteRegex.Replace(result, SanitizeUrlMatchSingleQuoted);
        result = DangerousHrefSrcBareRegex.Replace(result, SanitizeUrlMatchBare);

        // Small cleanup between tags
        result = ExcessWhitespaceRegex.Replace(result, "><");

        return result.Trim();
    }

    public bool IsSafeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var value = url.Trim();

        // Allow relative URLs
        if (value.StartsWith("/", StringComparison.Ordinal))
            return true;

        if (value.StartsWith("#", StringComparison.Ordinal))
            return true;

        if (value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private string SanitizeUrlMatchDoubleQuoted(Match match)
    {
        var attrName = match.Groups[1].Value;
        var rawUrl = match.Groups[2].Value;

        return IsSafeUrl(rawUrl)
            ? $@" {attrName}=""{rawUrl.Trim()}"""
            : string.Empty;
    }

    private string SanitizeUrlMatchSingleQuoted(Match match)
    {
        var attrName = match.Groups[1].Value;
        var rawUrl = match.Groups[2].Value;

        return IsSafeUrl(rawUrl)
            ? $@" {attrName}='{rawUrl.Trim()}'"
            : string.Empty;
    }

    private string SanitizeUrlMatchBare(Match match)
    {
        var attrName = match.Groups[1].Value;
        var rawUrl = match.Groups[2].Value.Trim();

        return IsSafeUrl(rawUrl)
            ? $@" {attrName}=""{rawUrl}"""
            : string.Empty;
    }
}
