using CardMaker.Application.Assets;
using CardMaker.Rendering.Fonts;
using Microsoft.AspNetCore.Mvc;

namespace CardMaker.Web.Endpoints;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
}

public static class AssetEndpoints
{
    /// <summary>
    /// Gli asset non stanno in wwwroot: passano da qui per essere soggetti ad autorizzazione (ADR-005).
    /// </summary>
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/assets").RequireAuthorization();

        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IAssetCatalog assets,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var asset = await assets.FindAsync(id, cancellationToken);
            if (asset is null)
            {
                return Results.NotFound();
            }

            var content = await assets.OpenContentAsync(asset.Sha256, cancellationToken);
            if (content is null)
            {
                return Results.NotFound();
            }

            http.Response.Headers["X-Content-Type-Options"] = "nosniff";
            http.Response.Headers.CacheControl = "private, max-age=31536000, immutable";

            return Results.File(content, asset.ContentType, enableRangeProcessing: true);
        });

        var fonts = endpoints.MapGroup("/fonts").RequireAuthorization(AuthorizationPolicies.AdminOnly);

        fonts.MapGet("/{id:guid}/preview.png", async (
            Guid id,
            [FromServices] IFontCatalog catalog,
            [FromServices] FontPreviewRenderer renderer,
            [FromQuery] string? sample,
            CancellationToken cancellationToken) =>
        {
            var bytes = await catalog.GetBytesAsync(id, cancellationToken);
            if (bytes is null)
            {
                return Results.NotFound();
            }

            return Results.File(renderer.Render(bytes, sample), "image/png");
        });

        return endpoints;
    }
}
