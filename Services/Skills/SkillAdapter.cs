using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OpenClaw.Windows.Services.Tools;

namespace OpenClaw.Windows.Services.Skills
{
    public class SkillAdapter : IAiTool
    {
        private readonly ISkill _skill;

        public SkillAdapter(ISkill skill)
        {
            _skill = skill;
        }

        public string Name => _skill.Name;

        public string Description => _skill.Description;

        public object Parameters => JsonSerializer.Deserialize<object>(_skill.ParametersJson) ?? new { };

        public bool IsUnsafe => true; // All external skills are treated as unsafe by default

        public async Task<string> ExecuteAsync(string jsonArgs)
        {
            var args = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(jsonArgs))
            {
                try 
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonArgs);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            args[kvp.Key] = kvp.Value.ToString();
                        }
                    }
                }
                catch
                {
                    // Fallback if args are not a flat dictionary
                    args["arguments"] = jsonArgs;
                }
            }

            return await _skill.ExecuteAsync(args);
        }
    }
}
