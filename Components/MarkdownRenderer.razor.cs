using Microsoft.AspNetCore.Components;
using System.Text;

namespace NetworkMonitorAgent.Components
{
    public partial class MarkdownRenderer
    {
        [Parameter]
        public string Content { get; set; }

        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var result = new StringBuilder();
            var lines = markdown.Split('\n');
            bool inCodeBlock = false;
            bool inUnorderedList = false;
            bool inOrderedList = false;
            bool inBlockquote = false;
            bool inTable = false;
            List<string> tableLines = new List<string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    // Close any open blocks
                    if (inUnorderedList)
                    {
                        result.AppendLine("</ul>");
                        inUnorderedList = false;
                    }
                    if (inOrderedList)
                    {
                        result.AppendLine("</ol>");
                        inOrderedList = false;
                    }
                    if (inBlockquote)
                    {
                        result.AppendLine("</blockquote>");
                        inBlockquote = false;
                    }
                    if (inTable && tableLines.Count > 0)
                    {
                        result.AppendLine(ProcessTable(tableLines));
                        tableLines.Clear();
                        inTable = false;
                    }
                    
                    result.AppendLine("<br/>");
                    continue;
                }

                // Handle code blocks
                if (line.StartsWith("```"))
                {
                    if (inCodeBlock)
                    {
                        result.AppendLine("</code></pre>");
                        inCodeBlock = false;
                    }
                    else
                    {
                        string language = line.Length > 3 ? line.Substring(3).Trim() : "";
                        result.AppendLine($"<pre><code class=\"language-{language}\">");
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    result.AppendLine(line);
                    continue;
                }

                // Handle tables
                if (line.Contains("|") && (line.Trim().StartsWith("|") || line.Contains("---")))
                {
                    if (!inTable)
                    {
                        inTable = true;
                    }
                    tableLines.Add(line);
                    continue;
                }
                else if (inTable && tableLines.Count > 0)
                {
                    result.AppendLine(ProcessTable(tableLines));
                    tableLines.Clear();
                    inTable = false;
                }

                // Headers
                if (line.StartsWith("# "))
                {
                    result.AppendLine($"<h1>{ProcessInlineMarkdown(line.Substring(2))}</h1>");
                }
                else if (line.StartsWith("## "))
                {
                    result.AppendLine($"<h2>{ProcessInlineMarkdown(line.Substring(3))}</h2>");
                }
                else if (line.StartsWith("### "))
                {
                    result.AppendLine($"<h3>{ProcessInlineMarkdown(line.Substring(4))}</h3>");
                }
                else if (line.StartsWith("#### "))
                {
                    result.AppendLine($"<h4>{ProcessInlineMarkdown(line.Substring(5))}</h4>");
                }
                else if (line.StartsWith("##### "))
                {
                    result.AppendLine($"<h5>{ProcessInlineMarkdown(line.Substring(6))}</h5>");
                }
                else if (line.StartsWith("###### "))
                {
                    result.AppendLine($"<h6>{ProcessInlineMarkdown(line.Substring(7))}</h6>");
                }
                // Alternative headers
                else if (line.StartsWith("===") && lines[Array.IndexOf(lines, line) - 1]?.Trim().Length > 0)
                {
                    string headerText = lines[Array.IndexOf(lines, line) - 1];
                    result.Remove(result.Length - headerText.Length - 1, headerText.Length + 1);
                    result.AppendLine($"<h1>{ProcessInlineMarkdown(headerText)}</h1>");
                }
                else if (line.StartsWith("---") && lines[Array.IndexOf(lines, line) - 1]?.Trim().Length > 0)
                {
                    string headerText = lines[Array.IndexOf(lines, line) - 1];
                    result.Remove(result.Length - headerText.Length - 1, headerText.Length + 1);
                    result.AppendLine($"<h2>{ProcessInlineMarkdown(headerText)}</h2>");
                }
                // Lists
                else if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
                {
                    if (!inUnorderedList)
                    {
                        result.AppendLine("<ul>");
                        inUnorderedList = true;
                    }
                    result.AppendLine($"<li>{ProcessInlineMarkdown(line.Substring(2))}</li>");
                }
                else if (char.IsDigit(line.TrimStart()[0]) && line.TrimStart().Contains(". "))
                {
                    if (!inOrderedList)
                    {
                        result.AppendLine("<ol>");
                        inOrderedList = true;
                    }
                    int spaceIndex = line.IndexOf(". ") + 2;
                    result.AppendLine($"<li>{ProcessInlineMarkdown(line.Substring(spaceIndex))}</li>");
                }
                // Blockquotes
                else if (line.StartsWith("> "))
                {
                    if (!inBlockquote)
                    {
                        result.AppendLine("<blockquote>");
                        inBlockquote = true;
                    }
                    result.AppendLine($"<p>{ProcessInlineMarkdown(line.Substring(2))}</p>");
                }
                else if (line.StartsWith(">"))
                {
                    if (!inBlockquote)
                    {
                        result.AppendLine("<blockquote>");
                        inBlockquote = true;
                    }
                    result.AppendLine($"<p>{ProcessInlineMarkdown(line.Substring(1))}</p>");
                }
                // Horizontal rule
                else if (line.Trim() == "---" || line.Trim() == "***" || line.Trim() == "___")
                {
                    result.AppendLine("<hr/>");
                }
                // Images
                else if (line.Trim().StartsWith("!["))
                {
                    result.AppendLine(ProcessImage(line));
                }
                // Task lists
                else if (line.Trim().StartsWith("- [ ]") || line.Trim().StartsWith("- [x]") || line.Trim().StartsWith("- [X]"))
                {
                    if (!inUnorderedList)
                    {
                        result.AppendLine("<ul>");
                        inUnorderedList = true;
                    }
                    bool isChecked = line.ToLower().Contains("[x]");
                    string taskText = line.Substring(line.IndexOf("]") + 1).Trim();
                    result.AppendLine($"<li><input type=\"checkbox\" disabled {(isChecked ? "checked" : "")}/> {ProcessInlineMarkdown(taskText)}</li>");
                }
                // Code spans
                else if (line.Contains("`"))
                {
                    result.AppendLine($"<p>{ProcessInlineMarkdown(line)}</p>");
                }
                // Default paragraph
                else
                {
                    result.AppendLine($"<p>{ProcessInlineMarkdown(line)}</p>");
                }
            }

            // Close any remaining open blocks
            if (inUnorderedList)
            {
                result.AppendLine("</ul>");
            }
            if (inOrderedList)
            {
                result.AppendLine("</ol>");
            }
            if (inBlockquote)
            {
                result.AppendLine("</blockquote>");
            }
            if (inTable && tableLines.Count > 0)
            {
                result.AppendLine(ProcessTable(tableLines));
            }
            if (inCodeBlock)
            {
                result.AppendLine("</code></pre>");
            }

            return result.ToString();
        }

        private static string ProcessInlineMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Process images
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"!\[(.*?)\]\((.*?)\)",
                "<img src=\"$2\" alt=\"$1\" title=\"$1\"/>");

            // Process links with target _blank
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\[(.*?)\]\((.*?)\)\{_blank\}",
                "<a href=\"$2\" target=\"_blank\" rel=\"noopener noreferrer\">$1</a>");

            // Process standard links
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\[(.*?)\]\((.*?)\)",
                "<a href=\"$2\">$1</a>");

            // Process auto-links
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"<(https?://[^\s]+)>",
                "<a href=\"$1\">$1</a>");

            // Process bold/strong
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\*\*(.*?)\*\*",
                "<strong>$1</strong>");
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"__(.*?)__",
                "<strong>$1</strong>");

            // Process italic/emphasis
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\*(.*?)\*",
                "<em>$1</em>");
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"_(.*?)_",
                "<em>$1</em>");

            // Process strikethrough
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"~~(.*?)~~",
                "<del>$1</del>");

            // Process inline code
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"`(.*?)`",
                "<code>$1</code>");

            // Process line breaks (two spaces at end of line)
            text = text.Replace("  \n", "<br/>");

            return text;
        }

        private static string ProcessImage(string line)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"!\[(.*?)\]\((.*?)\)");
            if (match.Success)
            {
                string altText = match.Groups[1].Value;
                string imageUrl = match.Groups[2].Value;
                return $"<img src=\"{imageUrl}\" alt=\"{altText}\" title=\"{altText}\"/>";
            }
            return line;
        }

        private static string ProcessTable(List<string> tableLines)
        {
            if (tableLines.Count < 2) return string.Empty;

            var result = new StringBuilder();
            result.AppendLine("<table>");

            bool headerProcessed = false;
            bool separatorProcessed = false;

            foreach (var line in tableLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cells = line.Split('|')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                if (cells.Count == 0) continue;

                // Check if this is the separator line
                if (cells.All(c => c.Replace("-", "").Trim().Length == 0))
                {
                    separatorProcessed = true;
                    continue;
                }

                result.AppendLine("<tr>");

                foreach (var cell in cells)
                {
                    string tag = (!headerProcessed && !separatorProcessed) ? "th" : "td";
                    
                    // Handle alignment
                    string style = "";
                    if (tableLines.Count > 1 && separatorProcessed && tableLines[1].Contains("|"))
                    {
                        var separatorCells = tableLines[1].Split('|')
                            .Select(c => c.Trim())
                            .Where(c => !string.IsNullOrEmpty(c))
                            .ToList();
                        
                        int cellIndex = cells.IndexOf(cell);
                        if (cellIndex < separatorCells.Count)
                        {
                            string separator = separatorCells[cellIndex];
                            if (separator.StartsWith(":") && separator.EndsWith(":"))
                                style = " style=\"text-align: center;\"";
                            else if (separator.StartsWith(":"))
                                style = " style=\"text-align: left;\"";
                            else if (separator.EndsWith(":"))
                                style = " style=\"text-align: right;\"";
                        }
                    }

                    result.AppendLine($"<{tag}{style}>{ProcessInlineMarkdown(cell)}</{tag}>");
                }

                result.AppendLine("</tr>");

                if (!headerProcessed && separatorProcessed)
                {
                    headerProcessed = true;
                }
            }

            result.AppendLine("</table>");
            return result.ToString();
        }
    }
}