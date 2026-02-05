using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace OpenClaw.Windows.Services
{
    public class CoreAgentBackgroundService : BackgroundService
    {
        private readonly ToastService _toastService;
        private readonly FileWatcherService _fileWatcher;

        public CoreAgentBackgroundService(ToastService toastService, FileWatcherService fileWatcher)
        {
            _toastService = toastService;
            _fileWatcher = fileWatcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Say Hello (and prove notifications work)
            _toastService.ShowToast("Super Agent 🦸‍♂️", "I am active in the background.");
            
            // Start Sensor
            string watchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            _fileWatcher.StartWatching(watchPath);
            _fileWatcher.FileDetected += OnFileDetected;

            // Main Autonomy Loop
            while (!stoppingToken.IsCancellationRequested)
            {
                System.Diagnostics.Debug.WriteLine($"[Agent Heartbeat] {DateTime.Now}");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private void OnFileDetected(object? sender, string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            _toastService.ShowToast("Sensors Active 👁️", $"I noticed a new file: {fileName}. Want me to analyze it?");
        }
    }
}
