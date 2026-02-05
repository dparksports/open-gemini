using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services.Skills
{
    public class SkillService
    {
        private readonly string _skillsRoot;
        private readonly System.Collections.ObjectModel.ObservableCollection<ISkill> _loadedSkills = new();

        public SkillService()
        {
            _skillsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenClaw", "Skills");
            Directory.CreateDirectory(_skillsRoot);
        }

        public System.Collections.ObjectModel.ObservableCollection<ISkill> LoadedSkills => _loadedSkills;

        public IEnumerable<ISkill> GetSkills() => _loadedSkills;

        public async Task RefreshSkillsAsync()
        {
            _loadedSkills.Clear();
            var directories = Directory.GetDirectories(_skillsRoot);

            foreach (var dir in directories)
            {
                var skill = await LoadSkillFromDirectoryAsync(dir);
                if (skill != null)
                {
                    _loadedSkills.Add(skill);
                }
            }
        }

        private async Task<ISkill?> LoadSkillFromDirectoryAsync(string directory)
        {
            var skillFile = Path.Combine(directory, "SKILL.md");
            if (!File.Exists(skillFile)) return null;

            // 1. Parse Metadata
            var metadata = await ParseSkillMetadataAsync(skillFile);
            if (string.IsNullOrEmpty(metadata.Name)) return null;

            // 2. Determine Runtime
            var psScript = Path.Combine(directory, "script.ps1");
            var pyScript = Path.Combine(directory, "script.py");

            if (File.Exists(psScript))
            {
                return new PowerShellSkill(metadata.Name, metadata.Description, psScript);
            }
            else if (File.Exists(pyScript))
            {
                 return new PythonSkill(metadata.Name, metadata.Description, pyScript);
            }

            return null;
        }

        private async Task<(string Name, string Description)> ParseSkillMetadataAsync(string path)
        {
            try 
            {
                var content = await File.ReadAllTextAsync(path);
                var match = Regex.Match(content, @"^---\s*\r?\n(.*?)\r?\n---\s*\r?\n", RegexOptions.Singleline);
                
                if (match.Success)
                {
                    var yaml = match.Groups[1].Value;
                    var name = ExtractYamlValue(yaml, "name");
                    var description = ExtractYamlValue(yaml, "description");
                    return (name, description);
                }
            }
            catch { }

            return (Path.GetFileName(Path.GetDirectoryName(path)) ?? "Unknown", "No description.");
        }

        private string ExtractYamlValue(string yaml, string key)
        {
            var match = Regex.Match(yaml, $@"^{key}:\s*(.*)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }
    }
}
