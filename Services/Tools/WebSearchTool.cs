using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services.Tools
{
    public class WebSearchTool : IAiTool
    {
        public string Name => "web_search";
        public string Description => "Search the web for information using a query. Returns a list of results with titles and links.";
        public bool IsUnsafe => false; // Reading is safe

        private readonly HttpClient _httpClient;

        public WebSearchTool()
        {
            _httpClient = new HttpClient();
            // Mimic a browser to avoid some bot detection
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<string> ExecuteAsync(string jsonArgs)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonArgs);
                if (!doc.RootElement.TryGetProperty("query", out var queryProp)) 
                {
                    return "Error: arguments must contain 'query'";
                }
                var query = queryProp.GetString();

                if (string.IsNullOrWhiteSpace(query)) return "Error: query cannot be empty";

                var url = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
                
                var html = await _httpClient.GetStringAsync(url);
                
                // Parse HTML (Simple regex or string parsing to avoid HAP dependency in the *Tool* itself if possible, 
                // but we already added HAP for ReadWebPage, so let's use it if we can access it.
                // For simplicity and speed in this "Zero Config" tool, let's use HtmlAgilityPack here too as we added it to the project.)
                
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(html);

                var results = new List<string>();
                int count = 0;

                // DuckDuckGo HTML structure usually has class 'result__a' for links
                // or 'result__body' for snippets.
                
                var resultNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'result')]");
                
                if (resultNodes != null)
                {
                    foreach (var node in resultNodes)
                    {
                        var titleNode = node.SelectSingleNode(".//a[contains(@class, 'result__a')]");
                        var snippetNode = node.SelectSingleNode(".//a[contains(@class, 'result__snippet')]");

                        if (titleNode != null)
                        {
                            var title = titleNode.InnerText.Trim();
                            var link = titleNode.GetAttributeValue("href", "");
                            var snippet = snippetNode?.InnerText.Trim() ?? "";

                            if (!string.IsNullOrEmpty(link) && !link.StartsWith("//duckduckgo"))
                            {
                                results.Add($"- [{title}]({link})\n  Snippet: {snippet}");
                                count++;
                            }
                        }
                        if (count >= 5) break;
                    }
                }

                if (count == 0) return "No results found.";

                return string.Join("\n\n", results);
            }
            catch (Exception ex)
            {
                return $"Error searching web: {ex.Message}";
            }
        }

        public object Parameters => new
        {
            type = "OBJECT",
            properties = new
            {
                query = new
                {
                    type = "STRING",
                    description = "The search query (e.g., 'current time in Tokyo', 'latest news on AI')."
                }
            },
            required = new[] { "query" }
        };
    }
}
