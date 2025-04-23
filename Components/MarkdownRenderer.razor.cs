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

            foreach (var line in lines)
            {
                if (line.StartsWith("# "))
                {
                    result.AppendLine($"<h1>{line.Substring(2)}</h1>");
                }
                else if (line.StartsWith("## "))
                {
                    result.AppendLine($"<h2>{line.Substring(3)}</h2>");
                }
                else if (line.StartsWith("### "))
                {
                    result.AppendLine($"<h3>{line.Substring(4)}</h3>");
                }
                else if (line.StartsWith("```"))
                {
                    result.AppendLine("<pre><code>");
                }
                else if (line.StartsWith("`") && line.EndsWith("`"))
                {
                    result.AppendLine($"<code>{line.Trim('`')}</code>");
                }
                else if (line.StartsWith("- "))
                {
                    result.AppendLine($"<li>{line.Substring(2)}</li>");
                }
                else if (line.StartsWith("> "))
                {
                    result.AppendLine($"<blockquote>{line.Substring(2)}</blockquote>");
                }
                else
                {
                    // Simple markdown links
                    var processedLine = System.Text.RegularExpressions.Regex.Replace(
                        line,
                        @"\[(.*?)\]\((.*?)\)",
                        "<a href=\"$2\">$1</a>");
                    
                    // Bold and italic
                    processedLine = System.Text.RegularExpressions.Regex.Replace(
                        processedLine,
                        @"\*\*(.*?)\*\*",
                        "<strong>$1</strong>");
                    
                    processedLine = System.Text.RegularExpressions.Regex.Replace(
                        processedLine,
                        @"\*(.*?)\*",
                        "<em>$1</em>");

                    result.AppendLine($"<p>{processedLine}</p>");
                }
            }

            return result.ToString();
        }
    }
}