using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services;

public class SafetyService
{
    // Basic regex for common jailbreak patterns
    private static readonly Regex _jailbreakPattern = new Regex(
        @"(ignore previous instructions|system override|delete all files|embedded instructions|simulated .* mode)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Basic PII detection (Simplistic examples)
    private static readonly Regex _emailPattern = new Regex(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", 
        RegexOptions.Compiled);

    /// <summary>
    /// Analyzes the prompt for potential security risks.
    /// </summary>
    /// <returns>True if safe, False if risky</returns>
    public bool IsPromptSafe(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return true;

        if (_jailbreakPattern.IsMatch(prompt))
        {
            System.Diagnostics.Debug.WriteLine($"[Safety] Jailbreak attempt detected: {prompt}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Scrubs sensitive information from the prompt before sending to Cloud APIs.
    /// </summary>
    public string ScrubPii(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return prompt;

        // Replace emails with [EMAIL]
        string scrubbed = _emailPattern.Replace(prompt, "[EMAIL_REDACTED]");
        
        // Add more scrubbers here (Phone, SSN, etc)
        
        return scrubbed;
    }
}
