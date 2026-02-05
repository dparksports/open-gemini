using System.Threading.Tasks;
using System.Collections.Generic;

namespace OpenClaw.Windows.Services.Skills
{
    public interface ISkill
    {
        string Name { get; }
        string Description { get; }
        string ParametersJson { get; }
        Task<string> ExecuteAsync(Dictionary<string, object> arguments);
    }
}
