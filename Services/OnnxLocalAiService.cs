using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace OpenClaw.Windows.Services;

public class OnnxLocalAiService : IAiService, IDisposable
{
    private Model? _model;
    private Tokenizer? _tokenizer;
    private readonly string _modelPath;
    private bool _isInitialized;

    public event Action<string, double>? DownloadProgressChanged;

    public OnnxLocalAiService()
    {
        // Model is stored in the "Model" subdirectory of the app
        string appDir = AppContext.BaseDirectory;
        _modelPath = Path.Combine(appDir, "Model");
        // Model path will be determined dynamically in InitializeAsync
    }

    private readonly System.Threading.SemaphoreSlim _initLock = new(1, 1);

    public async Task InitializeAsync()
    {
        // Double-check locking pattern
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Model");
            
            // precise check for the data file which caused the crash
            bool modelExists = File.Exists(Path.Combine(modelPath, "model.onnx")) && 
                              File.Exists(Path.Combine(modelPath, "model.onnx.data"));

            if (!modelExists)
            {
                 var downloadService = new ModelDownloadService();
                 
                 var progressReporter = new Progress<double>(p => 
                 {
                     DownloadProgressChanged?.Invoke("Downloading Model...", p);
                 });

                 await downloadService.DownloadModelAsync(modelPath, null, progressReporter);
            }

            try 
            {
                _model = new Model(modelPath);
                _tokenizer = new Tokenizer(_model);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing ONNX model: {ex.Message}");
                throw;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(string systemPrompt, string userPrompt)
    {
        if (_model == null || _tokenizer == null)
        {
            await InitializeAsync();
        }

        var sequences = _tokenizer!.Encode($"<|system|>{systemPrompt}<|end|><|user|>{userPrompt}<|end|><|assistant|>");

        var generatorParams = new GeneratorParams(_model);
        generatorParams.SetSearchOption("max_length", 2048);
        
        using var generator = new Generator(_model, generatorParams);
        generator.AppendTokenSequences(sequences);

        while (!generator.IsDone())
        {
             string part = "";
             await Task.Run(() => 
             {
                 generator.GenerateNextToken();
                 // Decode only the last token
                 // Note: This logic assumes simple token-to-text mapping which is typical
                 // For robust streaming, ideally we keep track of previous length.
                 var outputSequences = generator.GetSequence(0);
                 var newToken = outputSequences[^1..];
                 part = _tokenizer.Decode(newToken);
             });

             yield return part;
        }
    }

    public async Task RedownloadModelAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            // Delete existing model files to force re-download
            if (Directory.Exists(_modelPath))
            {
                // We can't delete the directory if the model is loaded and locking files.
                // Disposition is needed.
                _model?.Dispose();
                _tokenizer?.Dispose();
                _model = null;
                _tokenizer = null;
                _isInitialized = false;

                try 
                {
                    Directory.Delete(_modelPath, true); 
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting model directory: {ex.Message}");
                    // Could be locked, but we'll try to download over it or handle it.
                }
            }
            
            var downloadService = new ModelDownloadService();
             
             var progressReporter = new Progress<double>(p => 
             {
                 DownloadProgressChanged?.Invoke("Downloading Model...", p);
             });

             await downloadService.DownloadModelAsync(_modelPath, null, progressReporter);

             // Re-initialize
            _model = new Model(_modelPath);
            _tokenizer = new Tokenizer(_model);
            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        _model?.Dispose();
        _tokenizer?.Dispose();
    }
}
