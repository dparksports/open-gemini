using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace OpenClaw.Windows.Services.Tools
{
    public class ReadTextFromImageTool : IAiTool
    {
        private readonly OcrService _ocrService;

        public ReadTextFromImageTool(OcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public string Name => "read_text_from_image";
        public string Description => "Extracts text from an image file using local OCR (Optical Character Recognition). Supported formats: jpg, png, bmp.";
        public bool IsUnsafe => false;

        public object Parameters => new
        {
            type = "OBJECT",
            properties = new
            {
                filePath = new
                {
                    type = "STRING",
                    description = "The absolute path to the local image file."
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

                var text = await _ocrService.RecognizeTextAsync(path);
                
                if (string.IsNullOrWhiteSpace(text)) return "[OCR Output: No text detected]";
                
                return $"[OCR Output]:\n{text}";
            }
            catch (Exception ex)
            {
                return $"Error executing OCR tool: {ex.Message}";
            }
        }
    }
}
