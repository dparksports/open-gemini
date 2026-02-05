using System;
using System.Threading.Tasks;
using SlackNet;
using SlackNet.Events;
using SlackNet.SocketMode;

namespace OpenClaw.Windows.Services;

public class SlackService : ISlackService
{
    private readonly string _appToken;
    private readonly string _botToken;
    private ISlackSocketModeClient? _client;

    public event EventHandler<string>? MessageReceived;

    public SlackService()
    {
        // In a real app, these should be securely stored or passed in.
        // For this POC, we will look for environment variables.
        _appToken = Environment.GetEnvironmentVariable("SLACK_APP_TOKEN") ?? "";
        _botToken = Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN") ?? "";
    }

    public async Task ConnectAsync()
    {
        if (string.IsNullOrEmpty(_appToken) || string.IsNullOrEmpty(_botToken))
        {
            System.Diagnostics.Debug.WriteLine("Slack Tokens missing. Skipping Slack connection.");
            return;
        }

        var serviceBuilder = new SlackServiceBuilder()
            .UseApiToken(_botToken)
            .UseAppLevelToken(_appToken);

        serviceBuilder.RegisterEventHandler(ctx => new MessageHandler(this));

        _client = serviceBuilder.GetSocketModeClient();
        await _client.Connect();
        
        System.Diagnostics.Debug.WriteLine("Connected to Slack!");
    }

    public async Task SendMessageAsync(string channelId, string message)
    {
        if (_client != null)
        {
            // We need a proper API client to send messages, SocketModeClient is mostly for receiving.
            // SlackServiceBuilder creates one for us, but for simplicity in this POC 
            // we will create a lightweight client helper or just use the one from the builder if accessible.
            // Actually, SlackNet's proper way is to use the API client.
            
            var api = new SlackApiClient(_botToken);
            await api.Chat.PostMessage(new SlackNet.WebApi.Message
            {
                Channel = channelId,
                Text = message
            });
        }
    }

    // Inner class to handle events
    private class MessageHandler : IEventHandler<MessageEvent>
    {
        private readonly SlackService _service;

        public MessageHandler(SlackService service)
        {
            _service = service;
        }

        public Task Handle(MessageEvent slackEvent)
        {
            // Ignore bot messages to avoid loops
            if (slackEvent.BotId != null) return Task.CompletedTask;

            _service.MessageReceived?.Invoke(_service, $"[{slackEvent.Channel}] {slackEvent.User}: {slackEvent.Text}");
            return Task.CompletedTask;
        }
    }
}
