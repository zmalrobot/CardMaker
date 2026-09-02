using System.Security.Cryptography;
using System.Text.Json;
using CardMaker.Application.Abstractions;
using CardMaker.Application.Admin;
using CardMaker.Application.Assets;
using CardMaker.Domain.Assets;
using CardMaker.Domain.Cards;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Identity;
using CardMaker.Domain.Options;
using CardMaker.Domain.Symbols;
using CardMaker.Infrastructure.Persistence;
using CardMaker.Infrastructure.Rendering;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CardMaker.Infrastructure.Admin;

public sealed class AdminContentService(
    CardMakerDbContext db,
    IAssetStore store) : IAdminContentService
{
    // ==========================================
    // 1. GIOCHI
    // ==========================================

    public async Task<IReadOnlyList<AdminGameDto>> GetGamesAsync(CancellationToken cancellationToken = default)
    {
        var games = await db.Games.AsNoTracking()
            .Include(g => g.CardTypes)
            .Include(g => g.SymbolSets)
            .Include(g => g.OptionLists)
            .Include(g => g.Traits)
            .OrderBy(g => g.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return games.Select(g => new AdminGameDto(
            g.Id,
            g.Key,
            g.Name.Get("it"),
            g.Name.Get("en"),
            g.Description.Get("it"),
            g.Description.Get("en"),
            g.WidthMm,
            g.HeightMm,
            g.BleedMm,
            g.SafeZoneMm,
            g.CornerRadiusMm,
            g.DefaultDpi,
            g.IsPublished,
            g.SortOrder,
            g.CardTypes.Count,
            g.SymbolSets.Count,
            g.OptionLists.Count,
            g.Traits.Count)).ToList();
    }

    public async Task<AdminGameDto?> GetGameByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var g = await db.Games.AsNoTracking()
            .Include(x => x.CardTypes)
            .Include(x => x.SymbolSets)
            .Include(x => x.OptionLists)
            .Include(x => x.Traits)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (g is null)
        {
            return null;
        }

        return new AdminGameDto(
            g.Id,
            g.Key,
            g.Name.Get("it"),
            g.Name.Get("en"),
            g.Description.Get("it"),
            g.Description.Get("en"),
            g.WidthMm,
            g.HeightMm,
            g.BleedMm,
            g.SafeZoneMm,
            g.CornerRadiusMm,
            g.DefaultDpi,
            g.IsPublished,
            g.SortOrder,
            g.CardTypes.Count,
            g.SymbolSets.Count,
            g.OptionLists.Count,
            g.Traits.Count);
    }

    public async Task<AdminGameDto> SaveGameAsync(SaveGameRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Game game;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            game = new Game { Key = request.Key };
            db.Games.Add(game);
        }
        else
        {
            game = await db.Games.FirstAsync(g => g.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            game.Key = request.Key;
            game.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        game.Name = LocalizedText.From(request.NameIt, request.NameEn);
        game.Description = LocalizedText.From(request.DescriptionIt, request.DescriptionEn);
        game.WidthMm = request.WidthMm;
        game.HeightMm = request.HeightMm;
        game.BleedMm = request.BleedMm;
        game.SafeZoneMm = request.SafeZoneMm;
        game.CornerRadiusMm = request.CornerRadiusMm;
        game.DefaultDpi = request.DefaultDpi;
        game.IsPublished = request.IsPublished;
        game.SortOrder = request.SortOrder;

        await LogAuditAsync(userId, isNew ? "Game.Create" : "Game.Update", "Game", game.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (await GetGameByIdAsync(game.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> DeleteGameAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id, cancellationToken).ConfigureAwait(false);
        if (game is null)
        {
            return false;
        }

        db.Games.Remove(game);
        await LogAuditAsync(userId, "Game.Delete", "Game", id.ToString(), game.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    // ==========================================
    // 2. TIPI DI CARTA (CARD TYPES)
    // ==========================================

    public async Task<IReadOnlyList<AdminCardTypeDto>> GetCardTypesAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var types = await db.CardTypes.AsNoTracking()
            .Include(c => c.Fields)
            .Include(c => c.Templates)
            .Include(c => c.AllowedTraits)
            .Where(c => c.GameId == gameId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return types.Select(c => new AdminCardTypeDto(
            c.Id,
            c.GameId,
            c.Key,
            c.Name.Get("it"),
            c.Name.Get("en"),
            c.SortOrder,
            c.Fields.Count,
            c.Templates.Count,
            c.AllowedTraits.Count)).ToList();
    }

    public async Task<AdminCardTypeDetailDto?> GetCardTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var c = await db.CardTypes.AsNoTracking()
            .Include(x => x.Fields)
            .Include(x => x.AllowedTraits)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (c is null)
        {
            return null;
        }

        var fields = c.Fields.OrderBy(f => f.SortOrder).Select(f => new AdminFieldDto(
            f.Id,
            f.CardTypeId,
            f.Key,
            f.Label.Get("it"),
            f.Label.Get("en"),
            f.HelpText.Get("it"),
            f.HelpText.Get("en"),
            f.Kind,
            f.IsRequired,
            f.DefaultValueJson,
            f.OptionListId,
            f.SymbolSetId,
            f.ValidationJson,
            f.ComputedExprJson,
            f.VisibleWhenJson,
            f.GroupName,
            f.SortOrder)).ToList();

        return new AdminCardTypeDetailDto(
            c.Id,
            c.GameId,
            c.Key,
            c.Name.Get("it"),
            c.Name.Get("en"),
            c.SortOrder,
            fields,
            c.AllowedTraits.Select(t => t.TraitId).ToList());
    }

    public async Task<AdminCardTypeDetailDto> SaveCardTypeAsync(SaveCardTypeRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        CardType ct;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            ct = new CardType { GameId = request.GameId, Key = request.Key };
            db.CardTypes.Add(ct);
        }
        else
        {
            ct = await db.CardTypes
                .Include(c => c.AllowedTraits)
                .FirstAsync(c => c.Id == request.Id!.Value, cancellationToken)
                .ConfigureAwait(false);
            ct.Key = request.Key;
            ct.UpdatedAtUtc = DateTimeOffset.UtcNow;
            ct.AllowedTraits.Clear();
        }

        ct.Name = LocalizedText.From(request.NameIt, request.NameEn);
        ct.SortOrder = request.SortOrder;

        foreach (var traitId in request.AllowedTraitIds)
        {
            ct.AllowedTraits.Add(new CardTypeTrait { CardTypeId = ct.Id, TraitId = traitId });
        }

        await LogAuditAsync(userId, isNew ? "CardType.Create" : "CardType.Update", "CardType", ct.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return (await GetCardTypeByIdAsync(ct.Id, cancellationToken).ConfigureAwait(false))!;
    }

    public async Task<bool> DeleteCardTypeAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var ct = await db.CardTypes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
        if (ct is null)
        {
            return false;
        }

        db.CardTypes.Remove(ct);
        await LogAuditAsync(userId, "CardType.Delete", "CardType", id.ToString(), ct.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    // ==========================================
    // 3. SCHEMA DEI CAMPI (FIELD DEFINITIONS)
    // ==========================================

    public async Task<AdminFieldDto> SaveFieldDefinitionAsync(Guid cardTypeId, SaveFieldDefinitionRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        FieldDefinition field;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            field = new FieldDefinition { CardTypeId = cardTypeId, Key = request.Key };
            db.FieldDefinitions.Add(field);
        }
        else
        {
            field = await db.FieldDefinitions.FirstAsync(f => f.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            field.Key = request.Key;
            field.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        field.Label = LocalizedText.From(request.LabelIt, request.LabelEn);
        field.HelpText = LocalizedText.From(request.HelpTextIt, request.HelpTextEn);
        field.Kind = request.Kind;
        field.IsRequired = request.IsRequired;
        field.DefaultValueJson = request.DefaultValueJson;
        field.OptionListId = request.OptionListId;
        field.SymbolSetId = request.SymbolSetId;
        field.ValidationJson = request.ValidationJson;
        field.ComputedExprJson = request.ComputedExprJson;
        field.VisibleWhenJson = request.VisibleWhenJson;
        field.GroupName = request.GroupName;
        field.SortOrder = request.SortOrder;

        await LogAuditAsync(userId, isNew ? "FieldDefinition.Create" : "FieldDefinition.Update", "FieldDefinition", field.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AdminFieldDto(
            field.Id,
            field.CardTypeId,
            field.Key,
            field.Label.Get("it"),
            field.Label.Get("en"),
            field.HelpText.Get("it"),
            field.HelpText.Get("en"),
            field.Kind,
            field.IsRequired,
            field.DefaultValueJson,
            field.OptionListId,
            field.SymbolSetId,
            field.ValidationJson,
            field.ComputedExprJson,
            field.VisibleWhenJson,
            field.GroupName,
            field.SortOrder);
    }

    public async Task<bool> DeleteFieldDefinitionAsync(Guid fieldId, string? userId, CancellationToken cancellationToken = default)
    {
        var field = await db.FieldDefinitions.FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken).ConfigureAwait(false);
        if (field is null)
        {
            return false;
        }

        db.FieldDefinitions.Remove(field);
        await LogAuditAsync(userId, "FieldDefinition.Delete", "FieldDefinition", fieldId.ToString(), field.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task ReorderFieldsAsync(Guid cardTypeId, IReadOnlyList<Guid> orderedFieldIds, string? userId, CancellationToken cancellationToken = default)
    {
        var fields = await db.FieldDefinitions
            .Where(f => f.CardTypeId == cardTypeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        for (int i = 0; i < orderedFieldIds.Count; i++)
        {
            var match = fields.FirstOrDefault(f => f.Id == orderedFieldIds[i]);
            if (match is not null)
            {
                match.SortOrder = (i + 1) * 10;
                match.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        await LogAuditAsync(userId, "FieldDefinition.Reorder", "FieldDefinition", cardTypeId.ToString(), JsonSerializer.Serialize(orderedFieldIds), cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ==========================================
    // 4. TRATTI (TRAITS)
    // ==========================================

    public async Task<IReadOnlyList<AdminTraitDto>> GetTraitsAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var traits = await db.Traits.AsNoTracking()
            .Where(t => t.GameId == gameId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return traits.Select(t => new AdminTraitDto(
            t.Id,
            t.GameId,
            t.Key,
            t.Name.Get("it"),
            t.Name.Get("en"),
            t.Group,
            t.SortOrder)).ToList();
    }

    public async Task<AdminTraitDto> SaveTraitAsync(SaveTraitRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Trait trait;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            trait = new Trait { GameId = request.GameId, Key = request.Key };
            db.Traits.Add(trait);
        }
        else
        {
            trait = await db.Traits.FirstAsync(t => t.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            trait.Key = request.Key;
            trait.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        trait.Name = LocalizedText.From(request.NameIt, request.NameEn);
        trait.Group = request.Group;
        trait.SortOrder = request.SortOrder;

        await LogAuditAsync(userId, isNew ? "Trait.Create" : "Trait.Update", "Trait", trait.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AdminTraitDto(
            trait.Id,
            trait.GameId,
            trait.Key,
            trait.Name.Get("it"),
            trait.Name.Get("en"),
            trait.Group,
            trait.SortOrder);
    }

    public async Task<bool> DeleteTraitAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var trait = await db.Traits.FirstOrDefaultAsync(t => t.Id == id, cancellationToken).ConfigureAwait(false);
        if (trait is null)
        {
            return false;
        }

        db.Traits.Remove(trait);
        await LogAuditAsync(userId, "Trait.Delete", "Trait", id.ToString(), trait.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    // ==========================================
    // 5. SIMBOLI (SYMBOL SETS & SYMBOLS)
    // ==========================================

    public async Task<IReadOnlyList<AdminSymbolSetDto>> GetSymbolSetsAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var sets = await db.SymbolSets.AsNoTracking()
            .Include(s => s.Symbols)
            .Where(s => s.GameId == gameId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sets.Select(s => new AdminSymbolSetDto(
            s.Id,
            s.GameId,
            s.Key,
            s.Name.Get("it"),
            s.Name.Get("en"),
            s.Symbols.OrderBy(sym => sym.SortOrder).Select(sym => new AdminSymbolDto(
                sym.Id,
                sym.SymbolSetId,
                sym.Key,
                sym.Name.Get("it"),
                sym.Name.Get("en"),
                sym.AssetId,
                sym.InlineToken,
                sym.SortOrder)).ToList())).ToList();
    }

    public async Task<AdminSymbolSetDto> SaveSymbolSetAsync(SaveSymbolSetRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        SymbolSet set;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            set = new SymbolSet { GameId = request.GameId, Key = request.Key };
            db.SymbolSets.Add(set);
        }
        else
        {
            set = await db.SymbolSets.Include(s => s.Symbols).FirstAsync(s => s.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            set.Key = request.Key;
            set.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        set.Name = LocalizedText.From(request.NameIt, request.NameEn);

        await LogAuditAsync(userId, isNew ? "SymbolSet.Create" : "SymbolSet.Update", "SymbolSet", set.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var symbols = set.Symbols.OrderBy(s => s.SortOrder).Select(sym => new AdminSymbolDto(
            sym.Id,
            sym.SymbolSetId,
            sym.Key,
            sym.Name.Get("it"),
            sym.Name.Get("en"),
            sym.AssetId,
            sym.InlineToken,
            sym.SortOrder)).ToList();

        return new AdminSymbolSetDto(set.Id, set.GameId, set.Key, set.Name.Get("it"), set.Name.Get("en"), symbols);
    }

    public async Task<bool> DeleteSymbolSetAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var set = await db.SymbolSets.FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
        if (set is null)
        {
            return false;
        }

        db.SymbolSets.Remove(set);
        await LogAuditAsync(userId, "SymbolSet.Delete", "SymbolSet", id.ToString(), set.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<AdminSymbolDto> SaveSymbolAsync(Guid symbolSetId, SaveSymbolRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Symbol symbol;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            symbol = new Symbol { SymbolSetId = symbolSetId, Key = request.Key };
            db.Symbols.Add(symbol);
        }
        else
        {
            symbol = await db.Symbols.FirstAsync(s => s.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            symbol.Key = request.Key;
            symbol.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        symbol.Name = LocalizedText.From(request.NameIt, request.NameEn);
        symbol.AssetId = request.AssetId;
        symbol.InlineToken = request.InlineToken;
        symbol.SortOrder = request.SortOrder;

        await LogAuditAsync(userId, isNew ? "Symbol.Create" : "Symbol.Update", "Symbol", symbol.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AdminSymbolDto(
            symbol.Id,
            symbol.SymbolSetId,
            symbol.Key,
            symbol.Name.Get("it"),
            symbol.Name.Get("en"),
            symbol.AssetId,
            symbol.InlineToken,
            symbol.SortOrder);
    }

    public async Task<bool> DeleteSymbolAsync(Guid symbolId, string? userId, CancellationToken cancellationToken = default)
    {
        var sym = await db.Symbols.FirstOrDefaultAsync(s => s.Id == symbolId, cancellationToken).ConfigureAwait(false);
        if (sym is null)
        {
            return false;
        }

        db.Symbols.Remove(sym);
        await LogAuditAsync(userId, "Symbol.Delete", "Symbol", symbolId.ToString(), sym.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    // ==========================================
    // 6. LISTE DI OPZIONI (OPTION LISTS & ITEMS)
    // ==========================================

    public async Task<IReadOnlyList<AdminOptionListDto>> GetOptionListsAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var lists = await db.OptionLists.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.GameId == gameId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return lists.Select(l => new AdminOptionListDto(
            l.Id,
            l.GameId,
            l.Key,
            l.Name.Get("it"),
            l.Name.Get("en"),
            l.Items.OrderBy(i => i.SortOrder).Select(i => new AdminOptionItemDto(
                i.Id,
                i.OptionListId,
                i.Key,
                i.Label.Get("it"),
                i.Label.Get("en"),
                i.SortOrder,
                i.MetadataJson,
                i.IsActive,
                i.SymbolId)).ToList())).ToList();
    }

    public async Task<AdminOptionListDto> SaveOptionListAsync(SaveOptionListRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        OptionList list;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            list = new OptionList { GameId = request.GameId, Key = request.Key };
            db.OptionLists.Add(list);
        }
        else
        {
            list = await db.OptionLists.Include(l => l.Items).FirstAsync(l => l.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            list.Key = request.Key;
            list.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        list.Name = LocalizedText.From(request.NameIt, request.NameEn);

        await LogAuditAsync(userId, isNew ? "OptionList.Create" : "OptionList.Update", "OptionList", list.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var items = list.Items.OrderBy(i => i.SortOrder).Select(i => new AdminOptionItemDto(
            i.Id,
            i.OptionListId,
            i.Key,
            i.Label.Get("it"),
            i.Label.Get("en"),
            i.SortOrder,
            i.MetadataJson,
            i.IsActive,
            i.SymbolId)).ToList();

        return new AdminOptionListDto(list.Id, list.GameId, list.Key, list.Name.Get("it"), list.Name.Get("en"), items);
    }

    public async Task<bool> DeleteOptionListAsync(Guid id, string? userId, CancellationToken cancellationToken = default)
    {
        var list = await db.OptionLists.FirstOrDefaultAsync(o => o.Id == id, cancellationToken).ConfigureAwait(false);
        if (list is null)
        {
            return false;
        }

        db.OptionLists.Remove(list);
        await LogAuditAsync(userId, "OptionList.Delete", "OptionList", id.ToString(), list.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<AdminOptionItemDto> SaveOptionItemAsync(Guid optionListId, SaveOptionItemRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        OptionItem item;
        bool isNew = !request.Id.HasValue || request.Id == Guid.Empty;

        if (isNew)
        {
            item = new OptionItem { OptionListId = optionListId, Key = request.Key };
            db.OptionItems.Add(item);
        }
        else
        {
            item = await db.OptionItems.FirstAsync(i => i.Id == request.Id!.Value, cancellationToken).ConfigureAwait(false);
            item.Key = request.Key;
            item.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        item.Label = LocalizedText.From(request.LabelIt, request.LabelEn);
        item.SortOrder = request.SortOrder;
        item.MetadataJson = request.MetadataJson;
        item.IsActive = request.IsActive;
        item.SymbolId = request.SymbolId;

        await LogAuditAsync(userId, isNew ? "OptionItem.Create" : "OptionItem.Update", "OptionItem", item.Id.ToString(), JsonSerializer.Serialize(request), cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AdminOptionItemDto(
            item.Id,
            item.OptionListId,
            item.Key,
            item.Label.Get("it"),
            item.Label.Get("en"),
            item.SortOrder,
            item.MetadataJson,
            item.IsActive,
            item.SymbolId);
    }

    public async Task<bool> DeleteOptionItemAsync(Guid optionItemId, string? userId, CancellationToken cancellationToken = default)
    {
        var item = await db.OptionItems.FirstOrDefaultAsync(i => i.Id == optionItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return false;
        }

        db.OptionItems.Remove(item);
        await LogAuditAsync(userId, "OptionItem.Delete", "OptionItem", optionItemId.ToString(), item.Key, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    // ==========================================
    // 7. OPERAZIONI SICURE SUGLI ASSET
    // ==========================================

    public async Task<AssetUsageCheckResult> CheckAssetUsageAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var reasons = new List<string>();

        // 1. Simboli
        var symbols = await db.Symbols.AsNoTracking().Where(s => s.AssetId == assetId).Select(s => s.Key).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (symbols.Count > 0)
        {
            reasons.Add($"Usato in {symbols.Count} simboli: {string.Join(", ", symbols)}");
        }

        // 2. Font
        var fonts = await db.FontAssets.AsNoTracking().Where(f => f.AssetId == assetId).Select(f => f.Alias).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (fonts.Count > 0)
        {
            reasons.Add($"Usato come font per il ruolo: {string.Join(", ", fonts)}");
        }

        // 3. CardType Icon
        var cardTypes = await db.CardTypes.AsNoTracking().Where(c => c.IconAssetId == assetId).Select(c => c.Key).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (cardTypes.Count > 0)
        {
            reasons.Add($"Usato come icona per i tipi di carta: {string.Join(", ", cardTypes)}");
        }

        // 4. Card Thumbnails
        var cardsCount = await db.Cards.AsNoTracking().CountAsync(c => c.ThumbnailAssetId == assetId, cancellationToken).ConfigureAwait(false);
        if (cardsCount > 0)
        {
            reasons.Add($"Usato come miniatura per {cardsCount} carte");
        }

        return new AssetUsageCheckResult(reasons.Count > 0, reasons);
    }

    public async Task<bool> SafeDeleteAssetAsync(Guid assetId, string? userId, CancellationToken cancellationToken = default)
    {
        var usage = await CheckAssetUsageAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (usage.IsInUse)
        {
            throw new InvalidOperationException($"Impossibile eliminare l'asset perché è in uso: {string.Join("; ", usage.UsageReasons)}");
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken).ConfigureAwait(false);
        if (asset is null)
        {
            return false;
        }

        db.Assets.Remove(asset);
        await LogAuditAsync(userId, "Asset.Delete", "Asset", assetId.ToString(), $"{asset.OriginalFileName} ({asset.Sha256})", cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Se nessun altro asset usa questo blob SHA-256, elimina il file
        var countWithSha = await db.Assets.CountAsync(a => a.Sha256 == asset.Sha256, cancellationToken).ConfigureAwait(false);
        if (countWithSha == 0)
        {
            await store.DeleteAsync(asset.Sha256, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<AssetReplaceResult> ReplaceAssetBlobAsync(Guid assetId, Stream newContent, string fileName, string? userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newContent);

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken).ConfigureAwait(false);
        if (asset is null)
        {
            return new AssetReplaceResult(false, null, $"Asset con ID '{assetId}' non trovato.");
        }

        using var memory = new MemoryStream();
        await newContent.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        var bytes = memory.ToArray();

        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        using var readStream = new MemoryStream(bytes);
        await store.SaveAsync(readStream, cancellationToken).ConfigureAwait(false);

        // Lettura dimensioni immagine se formato supportato
        int width = 0;
        int height = 0;
        try
        {
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec is not null)
            {
                width = codec.Info.Width;
                height = codec.Info.Height;
            }
        }
        catch
        {
            // Non è un'immagine standard
        }

        var oldSha = asset.Sha256;
        asset.Sha256 = sha256;
        asset.OriginalFileName = fileName;
        asset.ByteSize = bytes.Length;
        asset.PixelWidth = width;
        asset.PixelHeight = height;
        asset.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await LogAuditAsync(userId, "Asset.Replace", "Asset", assetId.ToString(), $"Da {oldSha} a {sha256} ({fileName})", cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AssetReplaceResult(true, sha256, null);
    }

    // ==========================================
    // 8. AUDIT LOG
    // ==========================================

    public async Task<IReadOnlyList<AuditLogEntryDto>> GetAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var logs = await db.AuditLog.AsNoTracking()
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return logs.Select(l => new AuditLogEntryDto(
            l.Id,
            l.UserId,
            l.Action,
            l.EntityName,
            l.EntityId,
            l.DetailsJson,
            l.IpAddress,
            l.CreatedAtUtc)).ToList();
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
}
