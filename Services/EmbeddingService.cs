using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace OpenClaw.Windows.Services
{
    public class EmbeddingService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey;
        private const string Model = "text-embedding-004";

        public EmbeddingService()
        {
            _httpClient = new HttpClient();
            _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                try
                {
                    // Fallback to reading secrets.json if env var is missing
                    var secretPath = Path.Combine(AppContext.BaseDirectory, "secrets.json");
                    if (File.Exists(secretPath))
                    {
                        var json = File.ReadAllText(secretPath);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("GeminiApiKey", out var prop))
                        {
                            _apiKey = prop.GetString() ?? "";
                        }
                    }
                }
                catch { }
            }
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
             if (string.IsNullOrEmpty(_apiKey)) throw new InvalidOperationException("API Key missing");
             if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();

             var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:embedContent?key={_apiKey}";
             
             var requestBody = new
             {
                 model = $"models/{Model}",
                 content = new { parts = new[] { new { text = text } } }
             };

             var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
             var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");

             var response = await _httpClient.PostAsync(url, content);
             var json = await response.Content.ReadAsStringAsync();

             if (!response.IsSuccessStatusCode)
             {
                 throw new Exception($"Embedding API Error: {json}");
             }

             using var doc = JsonDocument.Parse(json);
             // Response format: { "embedding": { "values": [0.1, ...] } }
             if (doc.RootElement.TryGetProperty("embedding", out var embedProp) && 
                 embedProp.TryGetProperty("values", out var valuesProp))
             {
                 var count = valuesProp.GetArrayLength();
                 var vector = new float[count];
                 int i = 0;
                 foreach (var val in valuesProp.EnumerateArray())
                 {
                     vector[i++] = val.GetSingle();
                 }
                 return vector;
             }

             return Array.Empty<float>();
        }
    }
}
