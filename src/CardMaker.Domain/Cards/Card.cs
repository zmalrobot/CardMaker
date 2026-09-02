using CardMaker.Domain.Assets;
using CardMaker.Domain.Common;
using CardMaker.Domain.Games;
using CardMaker.Domain.Templates;

namespace CardMaker.Domain.Cards;

/// <summary>
/// Una carta creata da un utente. E' legata a una versione specifica di template (ADR-007).
/// </summary>
public class Card : Entity
{
    public string OwnerUserId { get; set; } = string.Empty;

    public Guid GameId { get; set; }

    public Game Game { get; set; } = null!;

    public Guid CardTypeId { get; set; }

    public CardType CardType { get; set; } = null!;

    public Guid TemplateVersionId { get; set; }

    public TemplateVersion TemplateVersion { get; set; } = null!;

    public Guid? BackTemplateVersionId { get; set; }

    public TemplateVersion? BackTemplateVersion { get; set; }

    /// <summary>Titolo mostrato nella collezione personale, non necessariamente il nome sulla carta.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>JSON: dizionario chiave campo -> valore.</summary>
    public string ValuesJson { get; set; } = "{}";

    /// <summary>JSON: elenco delle chiavi dei trait selezionati.</summary>
    public string SelectedTraitsJson { get; set; } = "[]";

    public Guid? ThumbnailAssetId { get; set; }

    public Asset? ThumbnailAsset { get; set; }

    public ICollection<CardRender> Renders { get; set; } = [];
}

public enum RenderFormat
{
    Png = 0,
    Jpg = 1,
    Pdf = 2,
}

/// <summary>Render gia' prodotto, riutilizzabile finche' la chiave di cache resta valida.</summary>
public class CardRender : Entity
{
    public Guid CardId { get; set; }

    public Card Card { get; set; } = null!;

    public string CacheKey { get; set; } = string.Empty;

    public int Dpi { get; set; }

    public RenderFormat Format { get; set; }

    public CardFace Face { get; set; }

    public bool WithBleed { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public DateTimeOffset LastAccessedUtc { get; set; } = DateTimeOffset.UtcNow;
}
