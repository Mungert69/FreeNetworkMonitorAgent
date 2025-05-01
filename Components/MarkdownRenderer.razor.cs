using Microsoft.AspNetCore.Components;
using Markdig;

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

            // Configure the pipeline with all advanced extensions
            var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions() // Includes tables, task lists, etc.
    .UseEmojiAndSmiley()    // Support for emoji
    .UseMathematics()        // Support for math formulas
    .UseBootstrap()          // Adds Bootstrap classes
    .Build();

            return Markdown.ToHtml(markdown, pipeline);
        }
    }
}