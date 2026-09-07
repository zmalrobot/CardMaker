using System.Diagnostics;
using System.Text;
using CardMaker.AI.Abstractions;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.Extensions.Logging;

namespace CardMaker.AI.Engines;

/// <summary>
/// Implementazione thread-safe del motore di inferenza testuale basato su llama.cpp tramite LLamaSharp.
/// Gestisce il ciclo di vita dei pesi nativi in memoria e supporta cancellazione rapida e deallocazione pulita.
/// </summary>
public sealed class LlamaCppTextEngine : ITextGenerationEngine
{
    private readonly ILogger<LlamaCppTextEngine>? _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private LLamaWeights? _weights;
    private LLamaContext? _context;
    private InteractiveExecutor? _executor;
    private ModelParams? _currentParams;
    private string? _currentModelPath;
    private bool _disposed;

    public LlamaCppTextEngine(ILogger<LlamaCppTextEngine>? logger = null)
    {
        _logger = logger;
    }

    public string? ModelPath => _currentModelPath;

    public bool IsLoaded => _weights != null && !_disposed;

    public async Task LoadModelAsync(
        string modelPath,
        int contextSize = 2048,
        int threads = 0,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("Il percorso del modello non puo essere vuoto.", nameof(modelPath));
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"File del modello GGUF non trovato: {modelPath}", modelPath);
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_weights != null && string.Equals(_currentModelPath, modelPath, StringComparison.OrdinalIgnoreCase))
            {
                // Modello gia caricato in memoria con lo stesso path
                return;
            }

            // Rilascia eventuali pesi caricati precedentemente
            ReleaseModelResources();

            var cpuThreads = threads <= 0 ? Math.Max(1, Environment.ProcessorCount - 1) : threads;

            _logger?.LogInformation("Caricamento modello GGUF da '{ModelPath}' (Context: {ContextSize}, Threads: {Threads})...",
                modelPath, contextSize, cpuThreads);

            _currentParams = new ModelParams(modelPath)
            {
                ContextSize = (uint)contextSize,
                Threads = cpuThreads,
                GpuLayerCount = 0 // CPU backend predefinito multipiattaforma
            };

            // Caricamento pesi nativi da disco a RAM
            _weights = await Task.Run(() => LLamaWeights.LoadFromFile(_currentParams), cancellationToken).ConfigureAwait(false);
            _context = _weights.CreateContext(_currentParams);
            _executor = new InteractiveExecutor(_context);
            _currentModelPath = modelPath;

            _logger?.LogInformation("Modello GGUF caricato con successo in RAM.");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TextPromptResult> GenerateTextAsync(
        string modelPath,
        TextPromptRequest request,
        int contextSize = 2048,
        int threads = 0,
        IProgress<AiProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        progress?.Report(new AiProgressUpdate(AiProgressStage.Preparing, "Preparazione sessione di inferenza..."));

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_weights == null || !string.Equals(_currentModelPath, modelPath, StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(new AiProgressUpdate(AiProgressStage.LoadingModel, "Caricamento pesi del modello in RAM..."));

                ReleaseModelResources();

                if (!File.Exists(modelPath))
                {
                    throw new FileNotFoundException($"File del modello GGUF non trovato: {modelPath}", modelPath);
                }

                var cpuThreads = threads <= 0 ? Math.Max(1, Environment.ProcessorCount - 1) : threads;

                _currentParams = new ModelParams(modelPath)
                {
                    ContextSize = (uint)contextSize,
                    Threads = cpuThreads,
                    GpuLayerCount = 0
                };

                _weights = await Task.Run(() => LLamaWeights.LoadFromFile(_currentParams), cancellationToken).ConfigureAwait(false);
                _context = _weights.CreateContext(_currentParams);
                _executor = new InteractiveExecutor(_context);
                _currentModelPath = modelPath;
            }

            var promptBuilder = new StringBuilder();
            // Formattazione chat template standard compatibile con Gemma/Llama Chat
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                promptBuilder.Append("<start_of_turn>user\n")
                    .Append(request.SystemPrompt)
                    .Append("\n\n")
                    .Append(request.UserPrompt)
                    .Append("<end_of_turn>\n<start_of_turn>model\n");
            }
            else
            {
                promptBuilder.Append("<start_of_turn>user\n")
                    .Append(request.UserPrompt)
                    .Append("<end_of_turn>\n<start_of_turn>model\n");
            }

            var formattedPrompt = promptBuilder.ToString();

            var inferenceParams = new InferenceParams
            {
                MaxTokens = request.MaxTokens,
                SamplingPipeline = new DefaultSamplingPipeline
                {
                    Temperature = request.Temperature
                }
            };

            progress?.Report(new AiProgressUpdate(AiProgressStage.Generating, "Generazione in corso...", PartialToken: null));

            var stopwatch = Stopwatch.StartNew();
            var outputBuilder = new StringBuilder();
            var tokensCount = 0;

            if (_executor == null)
            {
                throw new InvalidOperationException("Inference executor non inizializzato.");
            }

            await foreach (var token in _executor.InferAsync(formattedPrompt, inferenceParams, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                outputBuilder.Append(token);
                tokensCount++;

                progress?.Report(new AiProgressUpdate(
                    AiProgressStage.Generating,
                    "Generazione in corso...",
                    PartialToken: token));
            }

            stopwatch.Stop();
            var rawText = outputBuilder.ToString();

            progress?.Report(new AiProgressUpdate(AiProgressStage.Completed, "Generazione completata con successo."));

            return new TextPromptResult(
                Text: rawText,
                DurationMs: stopwatch.ElapsedMilliseconds,
                TokensGenerated: tokensCount);
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new AiProgressUpdate(AiProgressStage.Idle, "Operazione interrotta dall'utente."));
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Errore durante l'inferenza LLM: {Message}", ex.Message);
            progress?.Report(new AiProgressUpdate(AiProgressStage.Error, $"Errore durante la generazione: {ex.Message}"));
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ResetContext()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _semaphore.Wait();
        try
        {
            if (_weights != null && _currentParams != null)
            {
                _context?.Dispose();
                _context = _weights.CreateContext(_currentParams);
                _executor = new InteractiveExecutor(_context);
                _logger?.LogDebug("Contesto di inferenza resettato con successo mantenendo i pesi del modello in RAM.");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UnloadModelAsync()
    {
        await _semaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            ReleaseModelResources();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ReleaseModelResources()
    {
        try
        {
            _context?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Eccezione durante il rilascio di LLamaContext.");
        }
        finally
        {
            _context = null;
            _executor = null;
        }

        try
        {
            _weights?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Eccezione durante il rilascio di LLamaWeights.");
        }
        finally
        {
            _weights = null;
            _currentParams = null;
            _currentModelPath = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ReleaseModelResources();
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
