using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace OpenClaw.Windows.Services.Tools
{
    public class TranscribeAudioTool : IAiTool
    {
        private readonly AudioTranscriberService _transcriberService;

        public TranscribeAudioTool(AudioTranscriberService transcriberService)
        {
            _transcriberService = transcriberService;
        }

        public string Name => "transcribe_audio_file";
        public string Description => "Transcribes a local audio or movie file into text using a local Whisper model. Supported formats: wav (16kHz), mp4, mp3 (may require conversion).";
        
        // Processing audio is safe and local
        public bool IsUnsafe => false;

        public object Parameters => new
        {
            type = "OBJECT",
            properties = new
            {
                filePath = new
                {
                    type = "STRING",
                    description = "The absolute path to the local audio or video file."
                }
            },
            required = new[] { "filePath" }
        };

        public async Task<string> ExecuteAsync(string jsonArgs)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonArgs);
                if (!doc.RootElement.TryGetProperty("filePath", out var pathProp))
                {
                    return "Error: arguments must contain 'filePath'";
                }
                var path = pathProp.GetString();

                if (string.IsNullOrWhiteSpace(path)) return "Error: filePath cannot be empty";

                if (!System.IO.File.Exists(path)) return $"Error: File not found at {path}";
                
                // TODO: Whisper.net might expect wav 16khz specifically.
                // Handling generic media might require ffmpeg or Windows.Media.Transcoding.
                // For this V1 tool, we assume the input is compatible or hope the library handles it.
                // If not, we return an error advising conversion.

                var text = await _transcriberService.TranscribeAudioAsync(path);
                
                if (string.IsNullOrWhiteSpace(text)) return "[Transcriber Output: No speech detected]";
                
                return $"[Transcriber Output]:\n{text}";
            }
            catch (Exception ex)
            {
                return $"Error executing Transcription tool: {ex.Message}";
            }
        }
    }
}
