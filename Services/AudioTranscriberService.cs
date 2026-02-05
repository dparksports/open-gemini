using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace OpenClaw.Windows.Services
{
    public class AudioTranscriberService
    {
        private readonly string _modelPath;
        private const string ModelUrl = "https://huggingface.co/sanchit-gandhi/whisper-small-ct2/resolve/main/ggml-model.bin"; // Fallback URL, usually we use built-in downloader
        
        // Using "Base" model for balance of speed/accuracy
        private const GgmlType ModelType = GgmlType.Base; 

        public AudioTranscriberService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenClaw", "Models");
            Directory.CreateDirectory(appData);
            _modelPath = Path.Combine(appData, "ggml-base.bin");
        }

        public async Task<string> TranscribeAudioAsync(string filePath)
        {
            if (!File.Exists(filePath)) return "Error: Audio file not found.";

            await EnsureModelExistsAsync();

            try
            {
                using var factory = WhisperFactory.FromPath(_modelPath);
                using var processor = factory.CreateBuilder()
                    .WithLanguage("auto")
                    .Build();

                using var fileStream = File.OpenRead(filePath);
                var sb = new System.Text.StringBuilder();

                // Simple transcription loop
                await foreach (var segment in processor.ProcessAsync(fileStream))
                {
                    sb.AppendLine(segment.Text);
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"Error during transcription: {ex.Message}";
            }
        }

        private async Task EnsureModelExistsAsync()
        {
            if (File.Exists(_modelPath)) return;

            // Simple downloader logic using Whisper.net's helper if available, or manual download
            // Whisper.net has a GgmlDownloader
            using var stream = await WhisperGgmlDownloader.GetGgmlModelAsync(ModelType);
            using var fileWriter = File.OpenWrite(_modelPath);
            await stream.CopyToAsync(fileWriter);
        }
    }
}
