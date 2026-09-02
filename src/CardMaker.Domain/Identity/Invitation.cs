using CardMaker.Domain.Common;

namespace CardMaker.Domain.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = [Admin, User];
}

/// <summary>
/// La registrazione libera non esiste: l'app e' esposta su internet e si entra solo su invito (ADR-012).
/// </summary>
public class Invitation : Entity
{
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash del token: il valore in chiaro esiste solo nel link inviato.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public string Role { get; set; } = AppRoles.User;

    public string? CreatedByUserId { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RedeemedAtUtc { get; set; }

    public string? RedeemedByUserId { get; set; }

    public bool IsRevoked { get; set; }

    public bool IsPending(DateTimeOffset now) => !IsRevoked && RedeemedAtUtc is null && ExpiresAtUtc > now;
}

public class AuditLogEntry : Entity
{
    public string? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? DetailsJson { get; set; }

    public string? IpAddress { get; set; }
}
