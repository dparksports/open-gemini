using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services;

public class HybridAiService : IAiService
{
    private readonly OnnxLocalAiService _localService;
    private readonly SafetyService _safetyService;
    public GoogleGeminiService CloudService { get; }

    public HybridAiService(OnnxLocalAiService localService, GoogleGeminiService cloudService, SafetyService safetyService)
    {
        _localService = localService;
        CloudService = cloudService;
        _safetyService = safetyService;
    }

    public async IAsyncEnumerable<OpenClaw.Windows.Models.AgentResponse> GetStreamingResponseAsync(string systemPrompt, string userPrompt)
    {
        // 1. Safety Check
        if (!_safetyService.IsPromptSafe(userPrompt))
        {
             yield return new OpenClaw.Windows.Models.AgentResponse { Text = "⚠️ Request blocked by Safety Protocols (Injection Detected)." };
             yield break;
        }

        // 2. PII Scrubbing
        string safePrompt = _safetyService.ScrubPii(userPrompt);

        bool isLocal = IsSimpleQuery(safePrompt);
        
        if (isLocal)
        {
            yield return new OpenClaw.Windows.Models.AgentResponse { Text = "[Local] 🦞 " };
            await foreach (var chunk in _localService.GetStreamingResponseAsync(systemPrompt, safePrompt))
            {
                yield return chunk;
            }
        }
        else
        {
             yield return new OpenClaw.Windows.Models.AgentResponse { Text = "[Gemini] ✨ " };
             await foreach (var chunk in CloudService.GetStreamingResponseAsync(systemPrompt, safePrompt))
             {
                 yield return chunk;
             }
        }
    }

    public async Task RedownloadModelAsync()
    {
        // For hybrid, we assume we want to redownload the local model if possible.
        if (_localService is OnnxLocalAiService local)
        {
            await local.RedownloadModelAsync();
        }
    }

    private bool IsSimpleQuery(string prompt)
    {
        // PoC Logic: Short queries go to Local, Long go to Gemini
        return prompt.Length < 50 || prompt.Contains("time", StringComparison.OrdinalIgnoreCase);
    }
}
