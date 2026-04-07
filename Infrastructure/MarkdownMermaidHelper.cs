using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace pearlxcore.dev.Infrastructure;

public static class MarkdownMermaidHelper
{
    private static readonly Regex MermaidBlockRegex = new(
        @"<pre><code class=""[^""]*\blanguage-mermaid\b[^""]*"">(?<content>[\s\S]*?)</code></pre>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex MermaidNodeLabelRegex = new(
        @"(?<id>\b[A-Za-z_][A-Za-z0-9_]*)(?<open>[\[\(\{])(?<label>[^\[\]\(\)\{\}]*)?(?<close>[\]\)\}])",
        RegexOptions.Compiled);

    private static readonly string[] MermaidStarts =
    [
        "flowchart ",
        "graph ",
        "sequenceDiagram",
        "classDiagram",
        "stateDiagram",
        "erDiagram",
        "journey",
        "gantt",
        "pie ",
        "mindmap",
        "timeline"
    ];

    public static string PrepareMarkdown(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var builder = new StringBuilder();
        var inMermaid = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```mermaid", StringComparison.OrdinalIgnoreCase))
            {
                if (builder.Length > 0 && !EndsWithBlankLine(builder))
                {
                    builder.AppendLine();
                }

                builder.AppendLine("<div class=\"mermaid\">");
                inMermaid = true;
                continue;
            }

            if (trimmed == "```" && inMermaid)
            {
                builder.AppendLine("</div>");
                inMermaid = false;
                continue;
            }

            if (inMermaid)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    builder.AppendLine();
                    continue;
                }

                if (!IsMermaidContinuation(trimmed))
                {
                    builder.AppendLine("</div>");
                    inMermaid = false;
                    builder.AppendLine(line);
                    continue;
                }

                builder.AppendLine(NormalizeMermaidLine(line));
                continue;
            }

            if (IsMermaidStart(trimmed))
            {
                if (builder.Length > 0 && !EndsWithBlankLine(builder))
                {
                    builder.AppendLine();
                }

                builder.AppendLine("<div class=\"mermaid\">");
                builder.AppendLine(NormalizeMermaidLine(line));
                inMermaid = true;
                continue;
            }

            builder.AppendLine(line);
        }

        if (inMermaid)
        {
            builder.AppendLine("</div>");
        }

        return builder.ToString().TrimEnd();
    }

    public static string NormalizeMermaidBlocks(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return MermaidBlockRegex.Replace(html, match =>
        {
            var content = WebUtility.HtmlDecode(match.Groups["content"].Value);
            return $"<div class=\"mermaid\">{content}</div>";
        });
    }

    private static bool IsMermaidStart(string line)
    {
        return MermaidStarts.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EndsWithBlankLine(StringBuilder builder)
    {
        if (builder.Length == 0)
        {
            return true;
        }

        var text = builder.ToString();
        return text.EndsWith("\n\n", StringComparison.Ordinal) || text.EndsWith("\r\n\r\n", StringComparison.Ordinal);
    }

    private static bool IsMermaidContinuation(string trimmed)
    {
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return true;
        }

        if (trimmed.StartsWith("%%", StringComparison.Ordinal) ||
            trimmed.StartsWith("flowchart ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("graph ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("classDiagram", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("erDiagram", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("journey", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("gantt", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("pie ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("mindmap", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("timeline", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("subgraph ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("end", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("style ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("classDef ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("class ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("linkStyle ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("click ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("participant ", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("autonumber", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("accTitle:", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("accDescr:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Regex.IsMatch(trimmed, @"^[-.=:>o\s\w\[\]{}()|`""',]+$") && trimmed.Contains("-->", StringComparison.Ordinal))
        {
            return true;
        }

        if (Regex.IsMatch(trimmed, @"^[A-Za-z0-9_]+[\[\(\{].*[\]\)\}]$"))
        {
            return true;
        }

        return trimmed.Contains("|", StringComparison.Ordinal) || trimmed.Contains("->", StringComparison.Ordinal);
    }

    private static string NormalizeMermaidLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return line;
        }

        return MermaidNodeLabelRegex.Replace(line, match =>
        {
            var label = match.Groups["label"].Value.Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                return match.Value;
            }

            if (label.StartsWith('"') && label.EndsWith('"'))
            {
                return match.Value;
            }

            var escaped = WebUtility.HtmlEncode(label);
            return $"{match.Groups["id"].Value}{match.Groups["open"].Value}\"{escaped}\"{match.Groups["close"].Value}";
        });
    }

}
