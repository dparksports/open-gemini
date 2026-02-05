using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services.Skills
{
    public class PowerShellSkill : ISkill
    {
        public string Name { get; }
        public string Description { get; }
        public string ParametersJson { get; }
        private readonly string _scriptPath;

        public PowerShellSkill(string name, string description, string scriptPath)
        {
            Name = name;
            Description = description;
            _scriptPath = scriptPath;
            
            // Default generic parameters if not found
            // In a real implementation, we would parse the param() block of the PS script
            ParametersJson = JsonSerializer.Serialize(new
            {
                type = "object",
                properties = new
                {
                    arguments = new
                    {
                        type = "string",
                        description = "Space-separated arguments to pass to the script"
                    }
                }
            });
        }

        public async Task<string> ExecuteAsync(Dictionary<string, object> arguments)
        {
            var argsString = "";
            if (arguments.ContainsKey("arguments"))
            {
                argsString = arguments["arguments"]?.ToString() ?? "";
            }
            
            // Construct the Process
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\" {argsString}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(15000)) // 15s timeout
            {
                process.Kill();
                return "Error: Script timed out after 15 seconds.";
            }

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return $"Error (Exit Code {process.ExitCode}): {error}";
            }

            return string.IsNullOrWhiteSpace(output) ? "Script executed successfully (No Output)." : output.Trim();
        }
    }
}
