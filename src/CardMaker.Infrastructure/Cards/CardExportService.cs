using CardMaker.Application.Abstractions;
using CardMaker.Application.Assets;
using CardMaker.Application.Cards;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using CardMaker.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CardMaker.Infrastructure.Cards;

public sealed class CardExportService : ICardExportService
{
    private readonly CardMakerDbContext _db;
    private readonly IRenderResourceLoader _resourceLoader;
    private readonly CardRenderer _renderer;
    private readonly PdfExporter _pdfExporter;
    private readonly ILogger<CardExportService>? _logger;

    [ActivatorUtilitiesConstructor]
    public CardExportService(
        CardMakerDbContext db,
        IRenderResourceLoader resourceLoader,
        CardRenderer renderer,
        PdfExporter pdfExporter,
        ILogger<CardExportService>? logger = null)
    {
        _db = db;
        _resourceLoader = resourceLoader;
        _renderer = renderer;
        _pdfExporter = pdfExporter;
        _logger = logger;
    }

    public CardExportService(
        CardMakerDbContext db,
        IAssetStore store,
        IFontCatalog fonts,
        IDecodedImageCache imageCache,
        CardRenderer renderer,
        PdfExporter pdfExporter,
        ILogger<CardExportService>? logger = null)
        : this(db, new RenderResourceLoader(db, store, fonts, imageCache), renderer, pdfExporter, logger)
    {
    }

    private static readonly JsonSerializerOptions JsonOptions = LayoutSerializer.Options;

    public async Task<CardExportResult> ExportCardAsync(
        Guid cardId,
        string userId,
        CardExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(options);

        var card = await _db.Cards.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.TemplateVersion)
            .Include(c => c.BackTemplateVersion)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.OwnerUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (card is null)
        {
            return new CardExportResult(false, null, null, null, $"Carta con ID '{cardId}' non trovata.");
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, CardValue>>(card.ValuesJson, JsonOptions) ?? [];

        // 1. Risoluzione Front Layout
        var frontLayout = LayoutSerializer.Deserialize(card.TemplateVersion.LayoutJson);
        if (frontLayout is null)
        {
            return new CardExportResult(false, null, null, null, "Layout front non valido.");
        }

        var safeTitle = string.Join("_", card.Title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim().Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "card";
        }

        // 2. Export in base al formato
        if (options.Format == RenderFormat.Pdf)
        {
            CardRenderResult frontResult;
            CardRenderResult? backResult = null;

            if (options.BothFaces && card.BackTemplateVersion is not null)
            {
                var backLayout = LayoutSerializer.Deserialize(card.BackTemplateVersion.LayoutJson);
                if (backLayout is not null)
                {
                    // Carica risorse sequenzialmente per garantire thread-safety su DbContext (CON-001)
                    using var frontResources = await _resourceLoader.LoadResourcesAsync(frontLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
                    using var backResources = await _resourceLoader.LoadResourcesAsync(backLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);

                    // Esegue il render SkiaSharp in parallelo (CPU-bound) per dimezzare i tempi di esportazione (CON-001)
                    var frontTask = Task.Run(() => _renderer.Render(new CardRenderRequest
                    {
                        Layout = frontLayout,
                        Values = values,
                        Resources = frontResources,
                        Dpi = Math.Clamp(options.Dpi, 72, 1200),
                        IncludeBleed = options.IncludeBleed,
                        RoundCorners = options.RoundCorners,
                        Format = RenderOutputFormat.Png,
                    }), cancellationToken);

                    var backTask = Task.Run(() => _renderer.Render(new CardRenderRequest
                    {
                        Layout = backLayout,
                        Values = values,
                        Resources = backResources,
                        Dpi = Math.Clamp(options.Dpi, 72, 1200),
                        IncludeBleed = options.IncludeBleed,
                        RoundCorners = options.RoundCorners,
                        Format = RenderOutputFormat.Png,
                    }), cancellationToken);

                    await Task.WhenAll(frontTask, backTask).ConfigureAwait(false);
                    frontResult = await frontTask.ConfigureAwait(false);
                    backResult = await backTask.ConfigureAwait(false);
                }
                else
                {
                    using var frontResources = await _resourceLoader.LoadResourcesAsync(frontLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
                    frontResult = await Task.Run(() => _renderer.Render(new CardRenderRequest
                    {
                        Layout = frontLayout,
                        Values = values,
                        Resources = frontResources,
                        Dpi = Math.Clamp(options.Dpi, 72, 1200),
                        IncludeBleed = options.IncludeBleed,
                        RoundCorners = options.RoundCorners,
                        Format = RenderOutputFormat.Png,
                    }), cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                using var frontResources = await _resourceLoader.LoadResourcesAsync(frontLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
                frontResult = await Task.Run(() => _renderer.Render(new CardRenderRequest
                {
                    Layout = frontLayout,
                    Values = values,
                    Resources = frontResources,
                    Dpi = Math.Clamp(options.Dpi, 72, 1200),
                    IncludeBleed = options.IncludeBleed,
                    RoundCorners = options.RoundCorners,
                    Format = RenderOutputFormat.Png,
                }), cancellationToken).ConfigureAwait(false);
            }

            if (frontResult.Content is null || frontResult.Content.Length == 0)
            {
                return new CardExportResult(false, null, null, null, "Rendering del fronte non riuscito.");
            }

            var pdfBytes = await Task.Run(() => _pdfExporter.Export(frontResult, backResult), cancellationToken).ConfigureAwait(false);
            var pdfFileName = $"{safeTitle}.pdf";
            _logger?.LogInformation(
                "[Export] Generato '{FileName}' (PDF @ {Dpi} DPI, {Pages} facciate) | {SizeKb:F1} KB",
                pdfFileName,
                options.Dpi,
                backResult is not null ? 2 : 1,
                pdfBytes.Length / 1024.0);
            return new CardExportResult(true, pdfBytes, "application/pdf", pdfFileName, null);
        }

        if (options.Face == CardFace.Back && card.BackTemplateVersion is not null)
        {
            var backLayout = LayoutSerializer.Deserialize(card.BackTemplateVersion.LayoutJson);
            if (backLayout is not null)
            {
                using var backResources = await _resourceLoader.LoadResourcesAsync(backLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
                var backResult = await Task.Run(() => _renderer.Render(new CardRenderRequest
                {
                    Layout = backLayout,
                    Values = values,
                    Resources = backResources,
                    Dpi = Math.Clamp(options.Dpi, 72, 1200),
                    IncludeBleed = options.IncludeBleed,
                    RoundCorners = options.RoundCorners,
                    Format = options.Format == RenderFormat.Jpg ? RenderOutputFormat.Jpeg : RenderOutputFormat.Png,
                }), cancellationToken).ConfigureAwait(false);

                var ext = options.Format == RenderFormat.Jpg ? "jpg" : "png";
                var mime = options.Format == RenderFormat.Jpg ? "image/jpeg" : "image/png";
                var backFileName = $"{safeTitle}_back.{ext}";
                _logger?.LogInformation(
                    "[Export] Generato '{FileName}' ({Format} @ {Dpi} DPI) | {SizeKb:F1} KB",
                    backFileName,
                    options.Format,
                    options.Dpi,
                    (backResult.Content?.Length ?? 0) / 1024.0);
                return new CardExportResult(true, backResult.Content, mime, backFileName, null);
            }
        }

        using var frontRes = await _resourceLoader.LoadResourcesAsync(frontLayout, values, card.GameId, cancellationToken).ConfigureAwait(false);
        var singleFrontResult = await Task.Run(() => _renderer.Render(new CardRenderRequest
        {
            Layout = frontLayout,
            Values = values,
            Resources = frontRes,
            Dpi = Math.Clamp(options.Dpi, 72, 1200),
            IncludeBleed = options.IncludeBleed,
            RoundCorners = options.RoundCorners,
            Format = options.Format == RenderFormat.Jpg ? RenderOutputFormat.Jpeg : RenderOutputFormat.Png,
        }), cancellationToken).ConfigureAwait(false);

        if (singleFrontResult.Content is null || singleFrontResult.Content.Length == 0)
        {
            return new CardExportResult(false, null, null, null, "Rendering del fronte non riuscito.");
        }

        var extension = options.Format == RenderFormat.Jpg ? "jpg" : "png";
        var mimeType = options.Format == RenderFormat.Jpg ? "image/jpeg" : "image/png";
        var frontFileName = $"{safeTitle}.{extension}";
        _logger?.LogInformation(
            "[Export] Generato '{FileName}' ({Format} @ {Dpi} DPI) | {SizeKb:F1} KB",
            frontFileName,
            options.Format,
            options.Dpi,
            (singleFrontResult.Content?.Length ?? 0) / 1024.0);
        return new CardExportResult(true, singleFrontResult.Content, mimeType, frontFileName, null);
    }
}
