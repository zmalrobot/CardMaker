using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Ai;

/// <summary>
/// Downloader multipiattaforma per modelli AI GGUF con supporto a HTTP Range (resume),
/// verifica preliminare dello spazio disco, calcolo del progresso in tempo reale,
/// retry con backoff su errori transitori e validazione dell''header GGUF.
/// </summary>
public sealed class AiModelDownloader : IAiModelDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiModelDownloader>? _logger;

    private static readonly byte[] GgufMagic = [(byte)'G', (byte)'G', (byte)'U', (byte)'F'];

    public AiModelDownloader(HttpClient? httpClient = null, ILogger<AiModelDownloader>? logger = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        _logger = logger;
    }

    public async Task DownloadModelAsync(
        AiModelDefinition model,
        string targetFilePath,
        IProgress<AiModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFilePath);

        var downloadUrl = !string.IsNullOrWhiteSpace(model.DownloadUrlDirect)
            ? model.DownloadUrlDirect
            : model.DownloadUrl;

        if (string.IsNullOrWhiteSpace(downloadUrl) || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"URL di download del modello '{model.Key}' non valido o mancante.", nameof(model));
        }

        var dir = Path.GetDirectoryName(targetFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
            EnsureSufficientDiskSpace(dir, model);
        }

        var tempFilePath = targetFilePath + ".download";
        const int maxRetries = 3;
        var attempt = 0;

        while (true)
        {
            attempt++;
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await DownloadInternalAsync(model, uri, tempFilePath, progress, cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Download del modello '{ModelKey}' cancellato.", model.Key);
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientException(ex))
            {
                var delaySeconds = (int)Math.Pow(2, attempt);
                _logger?.LogWarning(ex, "Tentativo {Attempt}/{Max} fallito per il download di '{ModelKey}'. Nuovo tentativo tra {Sec}s...",
                    attempt, maxRetries, model.Key, delaySeconds);

                progress?.Report(new AiModelDownloadProgress(
                    ModelKey: model.Key,
                    ModelDisplayName: model.DisplayName,
                    BytesDownloaded: File.Exists(tempFilePath) ? new FileInfo(tempFilePath).Length : 0,
                    TotalBytes: model.ExpectedSizeBytes,
                    Percentage: 0,
                    BytesPerSecond: 0,
                    EstimatedRemaining: null,
                    StatusMessage: $"Errore transitorio di rete. Nuovo tentativo {attempt}/{maxRetries} tra {delaySeconds}s...",
                    State: AiModelReadinessStatus.Downloading));

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken).ConfigureAwait(false);
            }
        }

        // Validazione finale del file temporaneo prima della promozione a file effettivo
        if (!ValidateModelFile(model, tempFilePath, out var validationError))
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
                // Ignora errori di rimozione
            }

            throw new InvalidDataException($"Validazione del modello '{model.Key}' fallita: {validationError}");
        }

        // Spostamento atomico del file completato e validato
        File.Move(tempFilePath, targetFilePath, overwrite: true);
        _logger?.LogInformation("Modello '{ModelKey}' promosso con successo in '{Path}'.", model.Key, targetFilePath);
    }

    private async Task DownloadInternalAsync(
        AiModelDefinition model,
        Uri uri,
        string tempFilePath,
        IProgress<AiModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        long existingLength = 0;
        if (File.Exists(tempFilePath))
        {
            existingLength = new FileInfo(tempFilePath).Length;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var isResume = false;

        if (existingLength > 0 && (model.ExpectedSizeBytes <= 0 || existingLength < model.ExpectedSizeBytes))
        {
            request.Headers.Range = new RangeHeaderValue(existingLength, null);
            isResume = true;
            _logger?.LogInformation("Tentativo di resume del download da offset {Offset} byte...", existingLength);
        }

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            // Range non valido: reimposta file e ricomincia da 0
            _logger?.LogWarning("Header Range non soddisfacibile (416). Reset del file parziale e riavvio download completo.");
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            existingLength = 0;
            isResume = false;
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }

        long totalBytes;
        long bytesDownloaded;
        FileMode fileMode;

        if (isResume && response.StatusCode == HttpStatusCode.PartialContent)
        {
            fileMode = FileMode.Append;
            bytesDownloaded = existingLength;
            totalBytes = (response.Content.Headers.ContentRange?.Length) ?? model.ExpectedSizeBytes;
        }
        else
        {
            fileMode = FileMode.Create;
            bytesDownloaded = 0;
            totalBytes = response.Content.Headers.ContentLength ?? model.ExpectedSizeBytes;
        }

        if (totalBytes <= 0 && model.ExpectedSizeBytes > 0)
        {
            totalBytes = model.ExpectedSizeBytes;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var fileStream = new FileStream(tempFilePath, fileMode, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);

        var buffer = new byte[128 * 1024];
        var stopwatch = Stopwatch.StartNew();
        var lastReportTime = 0L;
        var bytesSinceLastReport = 0L;
        var bytesPerSecond = 0.0;
        var lastLoggedPercentage = -1;

        while (true)
        {
            var read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            bytesDownloaded += read;
            bytesSinceLastReport += read;

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            if (elapsedMs - lastReportTime >= 250 || bytesDownloaded == totalBytes)
            {
                var deltaSec = (elapsedMs - lastReportTime) / 1000.0;
                if (deltaSec > 0)
                {
                    bytesPerSecond = bytesSinceLastReport / deltaSec;
                }

                lastReportTime = elapsedMs;
                bytesSinceLastReport = 0;

                var percentage = totalBytes > 0 ? Math.Clamp(Math.Round((double)bytesDownloaded / totalBytes * 100.0, 1), 0.0, 100.0) : 0.0;
                TimeSpan? eta = (bytesPerSecond > 0 && totalBytes > bytesDownloaded)
                    ? TimeSpan.FromSeconds((totalBytes - bytesDownloaded) / bytesPerSecond)
                    : null;

                var currentMb = Math.Round((double)bytesDownloaded / (1024 * 1024), 1);
                var totalMb = Math.Round((double)totalBytes / (1024 * 1024), 1);
                var speedMb = Math.Round(bytesPerSecond / (1024 * 1024), 1);

                if ((int)percentage != lastLoggedPercentage && (int)percentage % 5 == 0)
                {
                    lastLoggedPercentage = (int)percentage;
                    _logger?.LogInformation("[CardMaker.AI] Download '{ModelKey}': {Percentage}% ({CurrentMb} MB / {TotalMb} MB) - {SpeedMb} MB/s",
                        model.Key, percentage, currentMb, totalMb, speedMb);
                    Console.WriteLine($"[CardMaker.AI] Download {model.DisplayName}: {percentage:F1}% ({currentMb} MB / {totalMb} MB) - {speedMb} MB/s");
                }

                progress?.Report(new AiModelDownloadProgress(
                    ModelKey: model.Key,
                    ModelDisplayName: model.DisplayName,
                    BytesDownloaded: bytesDownloaded,
                    TotalBytes: totalBytes,
                    Percentage: percentage,
                    BytesPerSecond: bytesPerSecond,
                    EstimatedRemaining: eta,
                    StatusMessage: $"Download in corso: {currentMb} MB / {totalMb} MB ({percentage}%)",
                    State: AiModelReadinessStatus.Downloading));
            }
        }
    }

    public bool ValidateModelFile(AiModelDefinition model, string targetFilePath, out string? validationError)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!File.Exists(targetFilePath))
        {
            validationError = $"File non presente nel percorso '{targetFilePath}'.";
            return false;
        }

        var fileInfo = new FileInfo(targetFilePath);

        // Verifica dimensione minima (almeno 50 MB per un modello GGUF compatto)
        if (fileInfo.Length < 50L * 1024L * 1024L)
        {
            validationError = $"La dimensione del file ({fileInfo.Length} byte) e troppo ridotta per un modello LLM valido.";
            return false;
        }

        // Verifica corrispondenza con la dimensione attesa (tolleranza del 10% se non fissata al byte)
        if (model.ExpectedSizeBytes > 0 && fileInfo.Length < (model.ExpectedSizeBytes * 0.85))
        {
            validationError = $"Il file sembra incompleto o troncato: dimensione attuale {fileInfo.Length} byte, attesa ~{model.ExpectedSizeBytes} byte.";
            return false;
        }

        // Verifica Magic Number GGUF nei primi 4 byte ('G', 'G', 'U', 'F')
        try
        {
            using var fs = new FileStream(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var header = new byte[4];
            var bytesRead = fs.Read(header, 0, 4);

            if (bytesRead < 4 || !header.SequenceEqual(GgufMagic))
            {
                validationError = "L'intestazione del file non corrisponde al formato binario GGUF valido (Magic Header 'GGUF' mancante).";
                return false;
            }
        }
        catch (Exception ex)
        {
            validationError = $"Impossibile leggere l'intestazione del file: {ex.Message}";
            return false;
        }

        // Verifica Checksum SHA-256 se dichiarato nel profilo
        if (!string.IsNullOrWhiteSpace(model.ExpectedSha256))
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var fs = File.OpenRead(targetFilePath);
                var hashBytes = sha256.ComputeHash(fs);
                var actualHex = Convert.ToHexString(hashBytes);

                if (!string.Equals(actualHex, model.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    validationError = $"Checksum SHA-256 non corrispondente. Atteso: {model.ExpectedSha256}, Calcolato: {actualHex}.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                validationError = $"Errore durante il calcolo del checksum SHA-256: {ex.Message}";
                return false;
            }
        }

        validationError = null;
        return true;
    }

    private static void EnsureSufficientDiskSpace(string directoryPath, AiModelDefinition model)
    {
        try
        {
            var fullPath = Path.GetFullPath(directoryPath);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
            {
                return;
            }

            var drive = new DriveInfo(root);
            var requiredSpace = model.MinFreeDiskSpaceBytes > 0
                ? model.MinFreeDiskSpaceBytes
                : (model.ExpectedSizeBytes > 0 ? model.ExpectedSizeBytes + (500L * 1024L * 1024L) : 2L * 1024L * 1024L * 1024L);

            if (drive.AvailableFreeSpace < requiredSpace)
            {
                var availableGb = Math.Round((double)drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
                var requiredGb = Math.Round((double)requiredSpace / (1024.0 * 1024.0 * 1024.0), 1);

                throw new IOException(
                    $"Spazio su disco insufficiente sul volume '{drive.Name}'. " +
                    $"Spazio libero: {availableGb} GB, Spazio minimo richiesto per il modello '{model.DisplayName}': {requiredGb} GB.");
            }
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception)
        {
            // Ignora eccezioni su sistemi con virtual mount non supportati da DriveInfo
        }
    }

    private static bool IsTransientException(Exception ex) =>
        ex is HttpRequestException ||
        ex is TimeoutException ||
        ex is IOException;
}
