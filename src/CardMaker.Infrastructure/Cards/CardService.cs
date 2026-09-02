using System.Text.Json;
using CardMaker.Application.Cards;
using CardMaker.Contracts.Layout;
using CardMaker.Domain.Cards;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Cards;

public sealed class CardService(CardMakerDbContext db) : ICardService
{
    private static readonly JsonSerializerOptions JsonOptions = LayoutSerializer.Options;

    public async Task<IReadOnlyList<CardSummaryDto>> GetUserCardsAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var cards = await db.Cards.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.CardType)
            .Where(c => c.OwnerUserId == userId)
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return cards.Select(c => new CardSummaryDto(
            c.Id,
            c.Title,
            c.Game.Key,
            c.Game.Name.Get(),
            c.CardType.Key,
            c.CardType.Name.Get(),
            c.ThumbnailAssetId,
            c.CreatedAtUtc,
            c.UpdatedAtUtc ?? c.CreatedAtUtc)).ToList();
    }

    public async Task<CardDetailDto?> GetCardAsync(Guid cardId, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var card = await db.Cards.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.CardType)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.OwnerUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (card is null)
        {
            return null;
        }

        var values = JsonSerializer.Deserialize<Dictionary<string, CardValue>>(card.ValuesJson, JsonOptions)
            ?? [];
        var traits = JsonSerializer.Deserialize<List<string>>(card.SelectedTraitsJson, JsonOptions)
            ?? [];

        return new CardDetailDto(
            card.Id,
            card.Title,
            card.GameId,
            card.Game.Key,
            card.CardTypeId,
            card.CardType.Key,
            card.TemplateVersionId,
            card.BackTemplateVersionId,
            values,
            traits,
            card.ThumbnailAssetId,
            card.CreatedAtUtc,
            card.UpdatedAtUtc ?? card.CreatedAtUtc);
    }

    public async Task<CardDetailDto> CreateCardAsync(SaveCardRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var valuesJson = JsonSerializer.Serialize(request.Values, JsonOptions);
        var traitsJson = JsonSerializer.Serialize(request.SelectedTraits, JsonOptions);

        var card = new Card
        {
            OwnerUserId = userId,
            Title = request.Title,
            GameId = request.GameId,
            CardTypeId = request.CardTypeId,
            TemplateVersionId = request.TemplateVersionId,
            BackTemplateVersionId = request.BackTemplateVersionId,
            ValuesJson = valuesJson,
            SelectedTraitsJson = traitsJson,
            ThumbnailAssetId = request.ThumbnailAssetId,
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var game = await db.Games.AsNoTracking().FirstAsync(g => g.Id == card.GameId, cancellationToken).ConfigureAwait(false);
        var cardType = await db.CardTypes.AsNoTracking().FirstAsync(c => c.Id == card.CardTypeId, cancellationToken).ConfigureAwait(false);

        return new CardDetailDto(
            card.Id,
            card.Title,
            card.GameId,
            game.Key,
            card.CardTypeId,
            cardType.Key,
            card.TemplateVersionId,
            card.BackTemplateVersionId,
            request.Values,
            request.SelectedTraits,
            card.ThumbnailAssetId,
            card.CreatedAtUtc,
            card.UpdatedAtUtc ?? card.CreatedAtUtc);
    }

    public async Task<CardDetailDto> UpdateCardAsync(Guid cardId, SaveCardRequest request, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var card = await db.Cards
            .Include(c => c.Game)
            .Include(c => c.CardType)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.OwnerUserId == userId, cancellationToken)
            .ConfigureAwait(false) ?? throw new KeyNotFoundException($"Carta con ID '{cardId}' non trovata per l'utente.");

        card.Title = request.Title;
        card.GameId = request.GameId;
        card.CardTypeId = request.CardTypeId;
        card.TemplateVersionId = request.TemplateVersionId;
        card.BackTemplateVersionId = request.BackTemplateVersionId;
        card.ValuesJson = JsonSerializer.Serialize(request.Values, JsonOptions);
        card.SelectedTraitsJson = JsonSerializer.Serialize(request.SelectedTraits, JsonOptions);
        card.ThumbnailAssetId = request.ThumbnailAssetId;
        card.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CardDetailDto(
            card.Id,
            card.Title,
            card.GameId,
            card.Game.Key,
            card.CardTypeId,
            card.CardType.Key,
            card.TemplateVersionId,
            card.BackTemplateVersionId,
            request.Values,
            request.SelectedTraits,
            card.ThumbnailAssetId,
            card.CreatedAtUtc,
            card.UpdatedAtUtc ?? card.CreatedAtUtc);
    }

    public async Task<CardDetailDto> DuplicateCardAsync(Guid cardId, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var existing = await db.Cards.AsNoTracking()
            .Include(c => c.Game)
            .Include(c => c.CardType)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.OwnerUserId == userId, cancellationToken)
            .ConfigureAwait(false) ?? throw new KeyNotFoundException($"Carta con ID '{cardId}' non trovata per l'utente.");

        var duplicate = new Card
        {
            OwnerUserId = userId,
            Title = $"{existing.Title} (Copia)",
            GameId = existing.GameId,
            CardTypeId = existing.CardTypeId,
            TemplateVersionId = existing.TemplateVersionId,
            BackTemplateVersionId = existing.BackTemplateVersionId,
            ValuesJson = existing.ValuesJson,
            SelectedTraitsJson = existing.SelectedTraitsJson,
            ThumbnailAssetId = existing.ThumbnailAssetId,
        };

        db.Cards.Add(duplicate);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var values = JsonSerializer.Deserialize<Dictionary<string, CardValue>>(duplicate.ValuesJson, JsonOptions) ?? [];
        var traits = JsonSerializer.Deserialize<List<string>>(duplicate.SelectedTraitsJson, JsonOptions) ?? [];

        return new CardDetailDto(
            duplicate.Id,
            duplicate.Title,
            duplicate.GameId,
            existing.Game.Key,
            duplicate.CardTypeId,
            existing.CardType.Key,
            duplicate.TemplateVersionId,
            duplicate.BackTemplateVersionId,
            values,
            traits,
            duplicate.ThumbnailAssetId,
            duplicate.CreatedAtUtc,
            duplicate.UpdatedAtUtc ?? duplicate.CreatedAtUtc);
    }

    public async Task<bool> DeleteCardAsync(Guid cardId, string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var card = await db.Cards
            .FirstOrDefaultAsync(c => c.Id == cardId && c.OwnerUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (card is null)
        {
            return false;
        }

        db.Cards.Remove(card);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<CardTypeDetailDto>> GetGameCardTypesAsync(string gameKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameKey);

        var game = await db.Games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Key == gameKey, cancellationToken)
            .ConfigureAwait(false) ?? throw new KeyNotFoundException($"Gioco '{gameKey}' non trovato.");

        var cardTypes = await db.CardTypes.AsNoTracking()
            .Include(c => c.Fields).ThenInclude(f => f.OptionList).ThenInclude(o => o!.Items)
            .Include(c => c.Fields).ThenInclude(f => f.SymbolSet).ThenInclude(s => s!.Symbols)
            .Include(c => c.AllowedTraits).ThenInclude(at => at.Trait)
            .Include(c => c.Templates).ThenInclude(t => t.Versions)
            .Where(c => c.GameId == game.Id)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return cardTypes.Select(ct => new CardTypeDetailDto(
            ct.Id,
            ct.GameId,
            ct.Key,
            ct.Name.Get(),
            ct.Fields.OrderBy(f => f.SortOrder).Select(f => new FieldDefinitionDto(
                f.Id,
                f.Key,
                f.Label.Get(),
                f.HelpText.Get(),
                f.Kind,
                f.IsRequired,
                f.DefaultValueJson,
                f.GroupName,
                f.SortOrder,
                f.VisibleWhenJson,
                f.OptionList?.Items.OrderBy(i => i.SortOrder).Select(i => new OptionItemDto(i.Key, i.Label.Get())).ToList() ?? [],
                f.SymbolSet?.Symbols.OrderBy(s => s.SortOrder).Select(s => new SymbolOptionDto(s.Key, s.Name.Get(), s.AssetId, s.InlineToken)).ToList() ?? [])).ToList(),
            ct.AllowedTraits.Select(t => new TraitOptionDto(t.Trait.Id, t.Trait.Key, t.Trait.Name.Get(), t.Trait.Group)).ToList(),
            ct.Templates.ToList())).ToList();
    }
}
