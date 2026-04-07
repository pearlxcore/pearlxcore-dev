using System.Text.RegularExpressions;
using Ganss.Xss;
using Markdig;

namespace pearlxcore.dev.Infrastructure;

public static class ProjectPresentationHelper
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    public static string RenderMarkdown(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var preparedMarkdown = MarkdownMermaidHelper.PrepareMarkdown(markdown);
        var html = Markdown.ToHtml(preparedMarkdown, MarkdownPipeline);
        var sanitized = Sanitizer.Sanitize(html);
        return MarkdownMermaidHelper.NormalizeMermaidBlocks(sanitized);
    }

    public static string GetExcerpt(string? markdown, int maxLength = 220)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var text = markdown;
        text = Regex.Replace(text, @"```[\s\S]*?```", " ");
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]+\)", "$1");
        text = Regex.Replace(text, @"\[(.*?)\]\([^)]+\)", "$1");
        text = Regex.Replace(text, @"<[^>]+>", " ");
        text = Regex.Replace(text, @"[#>*_`~\-]{1,}", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength).TrimEnd() + "...";
    }

    public static string GetLatestReleaseUrl(string? githubUrl)
    {
        if (string.IsNullOrWhiteSpace(githubUrl))
        {
            return string.Empty;
        }

        var trimmed = githubUrl.Trim().TrimEnd('/');
        if (trimmed.EndsWith("/releases/latest", StringComparison.OrdinalIgnoreCase) ||
            trimmed.EndsWith("/releases", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed.Contains("github.com", StringComparison.OrdinalIgnoreCase)
            ? $"{trimmed}/releases/latest"
            : trimmed;
    }

    public static string GetDownloadBadgeUrl(string? githubUrl)
    {
        var repo = GetOwnerRepo(githubUrl);
        return string.IsNullOrWhiteSpace(repo)
            ? string.Empty
            : $"https://img.shields.io/github/downloads/{repo}/total.svg";
    }

    public static string? GetOwnerRepo(string? githubUrl)
    {
        if (string.IsNullOrWhiteSpace(githubUrl))
        {
            return null;
        }

        var trimmed = githubUrl.Trim().TrimEnd('/');
        var match = Regex.Match(trimmed, @"github\.com/([^/]+/[^/]+)$", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        foreach (var tag in new[] { "div", "section", "pre", "code", "blockquote", "p", "br", "hr", "ul", "ol", "li", "span", "strong", "em", "a", "img", "h1", "h2", "h3", "h4", "h5", "h6" })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("id");
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("title");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.AllowedAttributes.Add("alt");

        return sanitizer;
    }
}
