using CardMaker.Domain.Cards;
using CardMaker.Domain.Common;

namespace CardMaker.Domain.Templates;

public enum CardFace
{
    Front = 0,
    Back = 1,
}

public enum CardOrientation
{
    Portrait = 0,
    Landscape = 1,
}

public enum TemplateStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

public class Template : Entity
{
    public Guid CardTypeId { get; set; }

    public CardType CardType { get; set; } = null!;

    public string Key { get; set; } = string.Empty;

    public LocalizedText Name { get; set; } = new();

    public CardFace Face { get; set; } = CardFace.Front;

    public CardOrientation Orientation { get; set; } = CardOrientation.Portrait;

    public bool IsDefault { get; set; }

    /// <summary>JSON: AST condizionale che sceglie questo template in base ai valori inseriti.</summary>
    public string? SelectionRuleJson { get; set; }

    public int SortOrder { get; set; }

    public ICollection<TemplateVersion> Versions { get; set; } = [];
}

/// <summary>
/// Una versione pubblicata e' immutabile: le carte gia' create non devono cambiare aspetto (ADR-007).
/// </summary>
public class TemplateVersion : Entity
{
    public Guid TemplateId { get; set; }

    public Template Template { get; set; } = null!;

    public int VersionNumber { get; set; } = 1;

    public TemplateStatus Status { get; set; } = TemplateStatus.Draft;

    /// <summary>Documento di layout validato da JSON Schema prima dell'esecuzione.</summary>
    public string LayoutJson { get; set; } = "{}";

    public string? ChangeNote { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }
}
