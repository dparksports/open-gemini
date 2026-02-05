using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services;

public interface IAiService
{
    IAsyncEnumerable<OpenClaw.Windows.Models.AgentResponse> GetStreamingResponseAsync(string systemPrompt, string userPrompt);
    Task RedownloadModelAsync();
}
