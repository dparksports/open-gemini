using System.Collections.Generic;

namespace OpenClaw.Windows.Models
{
    public class AgentResponse
    {
        public string? Text { get; set; }
        public List<FunctionCall>? FunctionCalls { get; set; }
    }
}
