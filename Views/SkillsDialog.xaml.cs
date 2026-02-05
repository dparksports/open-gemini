using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using OpenClaw.Windows.Services.Skills;
using System;
using Microsoft.UI.Xaml;

namespace OpenClaw.Windows.Views
{
    public sealed partial class SkillsDialog : ContentDialog
    {
        public ObservableCollection<ISkill> Skills { get; private set; }

        public SkillsDialog()
        {
            this.InitializeComponent();
            var skillService = App.Current.Host.Services.GetRequiredService<SkillService>();
            Skills = skillService.LoadedSkills;
            
            // Handle Reload (mapped to CloseButton for convenience, ideally separate button)
            this.CloseButtonClick += async (s, e) => 
            {
                // Prevent closing immediately if we want to show loading state
                // But simplified: we trigger reload and let dialog close.
                 var skillService = App.Current.Host.Services.GetRequiredService<SkillService>();
                 var toolRegistry = App.Current.Host.Services.GetRequiredService<Services.Tools.ToolRegistry>();
                 
                 await skillService.RefreshSkillsAsync();
                 
                 // Re-register
                 foreach (var skill in skillService.GetSkills())
                 {
                     toolRegistry.RegisterTool(new SkillAdapter(skill));
                 }
            };
        }
    }
}
