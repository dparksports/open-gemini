using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Text;

namespace OpenClaw.Windows.Services
{
    public class OcrService
    {
        private OcrEngine? _ocrEngine;

        public OcrService()
        {
            // Try to use the user's language, fallback to English
            _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (_ocrEngine == null)
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(new global::Windows.Globalization.Language("en-US"));
            }
        }

        public async Task<string> RecognizeTextAsync(string filePath)
        {
            if (_ocrEngine == null) return "Error: OCR Engine not initialized available.";

            try
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                using var stream = await file.OpenAsync(FileAccessMode.Read);
                var decoder = await BitmapDecoder.CreateAsync(stream);
                
                // OCR requires a software bitmap
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // It must be in a supported format (usually Bgra8)
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                    softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var result = await _ocrEngine.RecognizeAsync(softwareBitmap);
                
                var sb = new StringBuilder();
                foreach (var line in result.Lines)
                {
                    sb.AppendLine(line.Text);
                }

                return sb.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"Error performing OCR: {ex.Message}";
            }
        }
    }
}
