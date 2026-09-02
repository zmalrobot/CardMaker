using Microsoft.AspNetCore.Identity;

namespace CardMaker.Infrastructure.Identity;

/// <summary>
/// Utente applicativo. Vive in Infrastructure perche' serve sia all'host web sia a quello desktop.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public string PreferredCulture { get; set; } = "it";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
