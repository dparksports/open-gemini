using System;
using System.Threading.Tasks;

namespace OpenClaw.Windows.Services;

public interface ISlackService
{
    Task ConnectAsync();
    Task SendMessageAsync(string channelId, string message);
    event EventHandler<string> MessageReceived;
}
