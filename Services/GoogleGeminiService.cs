using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OpenClaw.Windows.Services;

public class GoogleGeminiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly Services.Tools.ToolRegistry? _toolRegistry;
    private string _apiKey;

    public GoogleGeminiService(Services.Tools.ToolRegistry? toolRegistry = null)
    {
        _httpClient = new HttpClient();
        _toolRegistry = toolRegistry;
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            try
            {
                var secretPath = System.IO.Path.Combine(AppContext.BaseDirectory, "secrets.json");
                if (System.IO.File.Exists(secretPath))
                {
                    var json = System.IO.File.ReadAllText(secretPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("GeminiApiKey", out var prop))
                    {
                        _apiKey = prop.GetString() ?? "";
                    }
                }
            }
            catch { /* Ignore config errors, api key remains empty */ }
        }
    }
    public string CurrentModel { get; set; } = "gemini-2.0-flash-lite";

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        if (string.IsNullOrEmpty(_apiKey)) return new List<string>();

        var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}";
        try 
        {
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var models = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var modelsElement))
            {
                foreach (var model in modelsElement.EnumerateArray())
                {
                    var name = model.GetProperty("name").GetString();
                    if (name != null && name.Contains("gemini") && !name.Contains("embedding") && !name.Contains("robotics") && !name.Contains("competitor"))
                    {
                        models.Add(name.Replace("models/", ""));
                    }
                }
            }
            return models.OrderByDescending(m => m).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to list models: {ex.Message}");
            return new List<string> { "gemini-1.5-flash", "gemini-1.5-pro" }; 
        }
    }

    public async IAsyncEnumerable<OpenClaw.Windows.Models.AgentResponse> GetStreamingResponseAsync(string systemPrompt, string userPrompt)
    {
        // For V1 stability, we will fetch the full response and yield it.
        var response = await GenerateContentAsync(systemPrompt + "\n\n" + userPrompt);
        
        if (response.Text != null)
        {
            // Simulate streaming for UI smoothness
            var words = response.Text.Split(' ');
            foreach (var word in words)
            {
                yield return new OpenClaw.Windows.Models.AgentResponse { Text = word + " " };
                await Task.Delay(10); 
            }
        }
        
        if (response.FunctionCalls != null && response.FunctionCalls.Count > 0)
        {
            yield return response;
        }
    }
    
    public async Task<OpenClaw.Windows.Models.AgentResponse> GenerateContentAsync(string prompt)
    {
         if (string.IsNullOrEmpty(_apiKey)) return new OpenClaw.Windows.Models.AgentResponse { Text = "GEMINI_API_KEY missing" };

         var url = $"https://generativelanguage.googleapis.com/v1beta/models/{CurrentModel}:generateContent?key={_apiKey}";
         
         object? toolsPayload = null;
         if (_toolRegistry != null)
         {
             var functions = _toolRegistry.GetGeminiFunctionDeclarations();
             toolsPayload = new[] 
             { 
                 new { function_declarations = functions } 
             };
         }

         var requestBody = new
         {
             contents = new[]
             {
                 new { role = "user", parts = new[] { new { text = prompt } } }
             },
             tools = toolsPayload
         };
         
         var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
         var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");

         var response = await _httpClient.PostAsync(url, content);
         var json = await response.Content.ReadAsStringAsync();
         
         if (!response.IsSuccessStatusCode) return new OpenClaw.Windows.Models.AgentResponse { Text = $"Error: {json}" };

         try 
         {
             using var doc = JsonDocument.Parse(json);
             var candidates = doc.RootElement.GetProperty("candidates");
             if (candidates.GetArrayLength() == 0) return new OpenClaw.Windows.Models.AgentResponse { Text = "No response candidates." };

             var contentPart = candidates[0].GetProperty("content").GetProperty("parts")[0];
             
             // Check for function call
             if (contentPart.TryGetProperty("functionCall", out var functionCall))
             {
                 var functionName = functionCall.GetProperty("name").GetString() ?? "unknown";
                 var args = functionCall.GetProperty("args").ToString(); // Get raw JSON of args
                 
                 return new OpenClaw.Windows.Models.AgentResponse 
                 { 
                     FunctionCalls = new List<OpenClaw.Windows.Models.FunctionCall> 
                     { 
                         new OpenClaw.Windows.Models.FunctionCall { Name = functionName, JsonArgs = args } 
                     } 
                 };
             }
             
             // Standard text
             if (contentPart.TryGetProperty("text", out var textProp))
             {
                 return new OpenClaw.Windows.Models.AgentResponse { Text = textProp.GetString() };
             }
             
             return new OpenClaw.Windows.Models.AgentResponse { Text = "Empty response." };
         }
         catch (Exception ex)
         {
             return new OpenClaw.Windows.Models.AgentResponse { Text = $"Error parsing Gemini response: {ex.Message}" };
         }
    }

    public async Task<OpenClaw.Windows.Models.AgentResponse> GenerateContentAsync(List<OpenClaw.Windows.Models.GeminiContent> history)
    {
         if (string.IsNullOrEmpty(_apiKey)) return new OpenClaw.Windows.Models.AgentResponse { Text = "GEMINI_API_KEY missing" };

         var url = $"https://generativelanguage.googleapis.com/v1beta/models/{CurrentModel}:generateContent?key={_apiKey}";
         
         object? toolsPayload = null;
         if (_toolRegistry != null)
         {
             var functions = _toolRegistry.GetGeminiFunctionDeclarations();
             toolsPayload = new[] 
             { 
                 new { function_declarations = functions } 
             };
         }

         var requestBody = new
         {
             contents = history,
             tools = toolsPayload
         };
         
         var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
         var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");

         var response = await _httpClient.PostAsync(url, content);
         var json = await response.Content.ReadAsStringAsync();
         
         if (!response.IsSuccessStatusCode) return new OpenClaw.Windows.Models.AgentResponse { Text = $"Error: {json}" };

         try 
         {
             using var doc = JsonDocument.Parse(json);
             var candidates = doc.RootElement.GetProperty("candidates");
             if (candidates.GetArrayLength() == 0) return new OpenClaw.Windows.Models.AgentResponse { Text = "No response candidates." };

             var contentPart = candidates[0].GetProperty("content").GetProperty("parts")[0];
             
             if (contentPart.TryGetProperty("functionCall", out var functionCall))
             {
                 var functionName = functionCall.GetProperty("name").GetString() ?? "unknown";
                 var args = functionCall.GetProperty("args").ToString(); 
                 
                 return new OpenClaw.Windows.Models.AgentResponse 
                 { 
                     FunctionCalls = new List<OpenClaw.Windows.Models.FunctionCall> 
                     { 
                         new OpenClaw.Windows.Models.FunctionCall { Name = functionName, JsonArgs = args } 
                     } 
                 };
             }
             
             if (contentPart.TryGetProperty("text", out var textProp))
             {
                 return new OpenClaw.Windows.Models.AgentResponse { Text = textProp.GetString() };
             }
             
             return new OpenClaw.Windows.Models.AgentResponse { Text = "Empty response." };
         }
         catch (Exception ex)
         {
             return new OpenClaw.Windows.Models.AgentResponse { Text = $"Error parsing Gemini response: {ex.Message}" };
         }
    }

    Task IAiService.RedownloadModelAsync() => Task.CompletedTask;
}
