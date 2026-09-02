using System.Text.Json;
using System.Text.RegularExpressions;
using CardMaker.Application.Admin;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Identity;
using CardMaker.Domain.Templates;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Admin;

public sealed partial class TemplateAdminService(CardMakerDbContext db) : ITemplateAdminService
{
    public async Task<IReadOnlyList<TemplateSummaryDto>> GetTemplatesByCardTypeAsync(Guid cardTypeId, CancellationToken cancellationToken = default)
    {
        var templates = await db.Templates.AsNoTracking()
            .Include(t => t.Versions)
            .Where(t => t.CardTypeId == cardTypeId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return templates.Select(t =>
        {
            var published = t.Versions.Where(v => v.Status == TemplateStatus.Published).OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            var latest = t.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

            return new TemplateSummaryDto(
                t.Id,
                t.CardTypeId,
                t.Key,
                t.Name.Get("it"),
                t.IsDefault,
                t.SelectionRuleJson,
                t.SortOrder,
                t.Versions.Count,
                published?.VersionNumber,
                latest?.VersionNumber ?? 0);
        }).ToList();
    }

    public async Task<TemplateDetailDto?> GetTemplateDetailAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var t = await db.Templates.AsNoTracking()
            .Include(x => x.CardType)
                .ThenInclude(ct => ct.Game)
            .Include(x => x.Versions)
            .FirstOrDefaultAsync(x => x.Id == templateId, cancellationToken)
            .ConfigureAwait(false);

        if (t is null)
        {
            return null;
        }

        var orderedVersions = t.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new TemplateVersionDto(
            v.Id,
            v.TemplateId,
            v.VersionNumber,
            v.LayoutJson,
            v.ChangeNote,
            v.Status == TemplateStatus.Published,
            v.PublishedAtUtc,
            v.CreatedByUserId,
            v.CreatedAtUtc)).ToList();

        var current = orderedVersions.FirstOrDefault(v => v.IsPublished) ?? orderedVersions.FirstOrDefault();

        return new TemplateDetailDto(
            t.Id,
            t.CardTypeId,
            t.CardType.Name.Get("it"),
            t.CardType.Game.Key,
            t.Key,
            t.Name.Get("it"),
            t.IsDefault,
            t.SelectionRuleJson,
            t.SortOrder,
            orderedVersions,
            current);
    }

    public async Task<TemplateDetailDto> SaveTemplateAsync(SaveTemplateRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Template template;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            template = new Template
            {
                CardTypeId = request.CardTypeId,
                Key = request.Key,
            };
            db.Templates.Add(template);
        }
        else
        {
            template = await db.Templates.Include(t => t.Versions).FirstAsync(t => t.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            template.Key = request.Key;
            template.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        template.Name = Domain.Common.LocalizedText.From(request.Name);
        template.IsDefault = request.IsDefault;
        template.SelectionRuleJson = request.SelectionRuleJson;
        template.SortOrder = request.SortOrder;

        if (request.IsDefault)
        {
            var others = await db.Templates
                .Where(t => t.CardTypeId == request.CardTypeId && t.Id != template.Id && t.IsDefault)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var other in others)
            {
                other.IsDefault = false;
            }
        }

        await LogAuditAsync(userId, isNew ? "Template.Create" : "Template.Update", "Template", template.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Se è un nuovo template, crea la versione iniziale v1
        if (isNew)
        {
            var emptyLayout = new CardLayout
            {
                Canvas = new CanvasDefinition
                {
                    WidthMm = 59,
                    HeightMm = 86,
                    BleedMm = 2,
                    SafeZoneMm = 3,
                },
                Layers = [],
            };

            await CreateVersionAsync(template.Id, LayoutSerializer.Serialize(emptyLayout), "Versione iniziale v1", userId, cancellationToken).ConfigureAwait(false);
        }

        return (await GetTemplateDetailAsync(template.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId, string? userId, CancellationToken cancellationToken = default)
    {
        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return false;
        }

        db.Templates.Remove(template);
        await LogAuditAsync(userId, "Template.Delete", "Template", templateId.ToString(), template.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<TemplateVersionDto> CreateVersionAsync(Guid templateId, string layoutJson, string? changeNote, string? userId, CancellationToken cancellationToken = default)
    {
        var template = await db.Templates.Include(t => t.Versions).FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            throw new InvalidOperationException($"Template con ID '{templateId}' non trovato.");
        }

        int nextVersion = (template.Versions.Max(v => (int?)v.VersionNumber) ?? 0) + 1;

        var version = new TemplateVersion
        {
            TemplateId = templateId,
            VersionNumber = nextVersion,
            LayoutJson = layoutJson,
            ChangeNote = changeNote,
            Status = nextVersion == 1 ? TemplateStatus.Published : TemplateStatus.Draft,
            PublishedAtUtc = nextVersion == 1 ? DateTimeOffset.UtcNow : null,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.TemplateVersions.Add(version);
        await LogAuditAsync(userId, "TemplateVersion.Create", "TemplateVersion", version.Id.ToString(), $"v{nextVersion} ({changeNote})", cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TemplateVersionDto(
            version.Id,
            version.TemplateId,
            version.VersionNumber,
            version.LayoutJson,
            version.ChangeNote,
            version.Status == TemplateStatus.Published,
            version.PublishedAtUtc,
            version.CreatedByUserId,
            version.CreatedAtUtc);
    }

    public async Task<TemplateVersionDto> PublishVersionAsync(Guid versionId, string? userId, CancellationToken cancellationToken = default)
    {
        var target = await db.TemplateVersions.FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            throw new InvalidOperationException($"Versione template '{versionId}' non trovata.");
        }

        var allVersions = await db.TemplateVersions.Where(v => v.TemplateId == target.TemplateId).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var v in allVersions)
        {
            if (v.Id == target.Id)
            {
                v.Status = TemplateStatus.Published;
                v.PublishedAtUtc = DateTimeOffset.UtcNow;
            }
            else if (v.Status == TemplateStatus.Published)
            {
                v.Status = TemplateStatus.Archived;
            }
        }

        await LogAuditAsync(userId, "TemplateVersion.Publish", "TemplateVersion", target.Id.ToString(), $"v{target.VersionNumber}", cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TemplateVersionDto(
            target.Id,
            target.TemplateId,
            target.VersionNumber,
            target.LayoutJson,
            target.ChangeNote,
            target.Status == TemplateStatus.Published,
            target.PublishedAtUtc,
            target.CreatedByUserId,
            target.CreatedAtUtc);
    }

    public async Task<LayoutValidationReport> ValidateLayoutAsync(Guid cardTypeId, CardLayout layout, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var issues = new List<LayoutValidationIssue>();

        var ct = await db.CardTypes.AsNoTracking()
            .Include(c => c.Fields)
            .Include(c => c.Game)
                .ThenInclude(g => g.Fonts)
            .FirstOrDefaultAsync(c => c.Id == cardTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (ct is null)
        {
            issues.Add(new LayoutValidationIssue("error", "cardtype.missing", "Tipo di carta non trovato.", null));
            return new LayoutValidationReport(false, issues);
        }

        var knownFieldKeys = ct.Fields.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownFontRoles = ct.Game.Fonts.Select(f => f.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var layer in layout.EnumerateLayers())
        {
            var layerName = string.IsNullOrEmpty(layer.Name) ? layer.GetType().Name : layer.Name;

            // 1. Limiti normalizzati (0..1)
            if (layer.Rect.Width <= 0 || layer.Rect.Height <= 0)
            {
                issues.Add(new LayoutValidationIssue("error", "layer.zero_size", $"Il layer '{layerName}' ha dimensioni nulle o negative.", layerName));
            }
            if (layer.Rect.X < -0.1 || layer.Rect.Y < -0.1 || layer.Rect.Right > 1.1 || layer.Rect.Bottom > 1.1)
            {
                issues.Add(new LayoutValidationIssue("warning", "layer.out_of_bounds", $"Il layer '{layerName}' si estende significativamente oltre i bordi della carta.", layerName));
            }

            // 2. Controllo Binding Text & RichText
            if (layer is TextLayer text)
            {
                var matches = TokenRegex().Matches(text.Source);
                foreach (Match m in matches)
                {
                    var tokenKey = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(tokenKey) && !knownFieldKeys.Contains(tokenKey))
                    {
                        issues.Add(new LayoutValidationIssue("warning", "text.binding_unmapped", $"Il campo '{tokenKey}' referenziato nel testo non esiste nello schema campi.", layerName));
                    }
                }
            }

            if (layer is RichTextLayer rich)
            {
                var matches = TokenRegex().Matches(rich.Source);
                foreach (Match m in matches)
                {
                    var tokenKey = m.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(tokenKey) && !knownFieldKeys.Contains(tokenKey))
                    {
                        issues.Add(new LayoutValidationIssue("warning", "text.binding_unmapped", $"Il campo '{tokenKey}' referenziato nel rich text non esiste nello schema campi.", layerName));
                    }
                }
            }

            // 3. ImageSlot
            if (layer is ImageSlotLayer slot)
            {
                if (!string.IsNullOrEmpty(slot.FieldKey) && !knownFieldKeys.Contains(slot.FieldKey))
                {
                    issues.Add(new LayoutValidationIssue("warning", "slot.key_unmapped", $"Il campo immagine '{slot.FieldKey}' non esiste nello schema.", layerName));
                }
            }

            // 4. SymbolRepeater
            if (layer is SymbolRepeaterLayer rep)
            {
                if (!string.IsNullOrEmpty(rep.FieldKey) && !knownFieldKeys.Contains(rep.FieldKey))
                {
                    issues.Add(new LayoutValidationIssue("warning", "repeater.count_unmapped", $"Il campo conteggio '{rep.FieldKey}' non esiste.", layerName));
                }
            }

            // 5. ToggleGroup
            if (layer is ToggleGroupLayer link)
            {
                if (!string.IsNullOrEmpty(link.FieldKey) && !knownFieldKeys.Contains(link.FieldKey))
                {
                    issues.Add(new LayoutValidationIssue("warning", "link.binding_unmapped", $"Il campo frecce/voci '{link.FieldKey}' non esiste.", layerName));
                }
            }
        }

        bool hasErrors = issues.Any(i => i.Severity == "error");
        return new LayoutValidationReport(!hasErrors, issues);
    }

    private async Task LogAuditAsync(string? userId, string action, string entityName, string? entityId, string? details, CancellationToken cancellationToken)
    {
        var entry = new AuditLogEntry
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            DetailsJson = details,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.AuditLog.Add(entry);
    }

    [GeneratedRegex(@"\{\{([^}]+)\}\}")]
    private static partial Regex TokenRegex();
}
