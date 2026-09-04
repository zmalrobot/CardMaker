using CardMaker.Application.Rendering;
using CardMaker.Contracts.Layout;
using CardMaker.Rendering;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Rendering;

public sealed class CardPreviewService(
    IRenderResourceLoader resourceLoader,
    CardRenderer renderer,
    ILogger<CardPreviewService>? logger = null) : ICardPreviewService
{
    public async Task<CardPreviewResult> RenderAsync(
        CardPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await Task.Run(async () =>
        {
            CardLayout? layout;
            try
            {
                layout = LayoutSerializer.Deserialize(request.LayoutJson);
            }
            catch (System.Text.Json.JsonException ex)
            {
                return CardPreviewResult.Fail([$"Layout non leggibile: {ex.Message}"]);
            }

            var validation = LayoutSerializer.Validate(layout);
            if (!validation.IsValid)
            {
                return CardPreviewResult.Fail([.. validation.Issues.Select(i => $"{i.Code}: {i.Message}")]);
            }

            using var resources = await resourceLoader.LoadResourcesAsync(layout!, request.Values, request.GameId, cancellationToken).ConfigureAwait(false);

            var result = renderer.Render(new CardRenderRequest
            {
                Layout = layout!,
                Values = request.Values,
                Resources = resources,
                Dpi = Math.Clamp(request.Dpi, 48, 1200),
                IncludeBleed = request.IncludeBleed,
                RoundCorners = request.RoundCorners,
                ShowGuides = request.ShowGuides,
                Format = string.Equals(request.Format, "jpg", StringComparison.OrdinalIgnoreCase)
                    ? RenderOutputFormat.Jpeg
                    : RenderOutputFormat.Png,
            });

            var sizeKb = (result.Content?.Length ?? 0) / 1024.0;
            logger?.LogInformation(
                "[Preview] Render {Width}x{Height} px ({Format} @ {Dpi} DPI) | {SizeKb:F1} KB | {DurationMs:F1} ms | {WarningCount} avvisi",
                result.WidthPx,
                result.HeightPx,
                request.Format,
                request.Dpi,
                sizeKb,
                result.Duration.TotalMilliseconds,
                result.Warnings.Count);

            return new CardPreviewResult(
                true,
                result.Content,
                result.ContentType,
                result.WidthPx,
                result.HeightPx,
                [.. result.Warnings.Select(w => new PreviewWarning(w.Code, w.Message, w.LayerId))],
                result.Duration.TotalMilliseconds,
                []);
        }, cancellationToken).ConfigureAwait(false);
    }
}
