using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CardMaker.Desktop.Services;

/// <summary>
/// Provider di autenticazione per l'host desktop (ADR-009).
/// Concede automaticamente il ruolo 'Admin' all'utente locale senza login o richieste di rete.
/// Confinato rigorosamente a questo assembly desktop: non esiste nel Web pubblico.
/// </summary>
public sealed class DesktopAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState LocalAdminState;

    static DesktopAuthenticationStateProvider()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Local Administrator"),
            new Claim(ClaimTypes.NameIdentifier, "desktop-local-admin"),
            new Claim(ClaimTypes.Email, "admin@local.cardmaker"),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        var identity = new ClaimsIdentity(claims, "PhotinoLocalBypass");
        var principal = new ClaimsPrincipal(identity);
        LocalAdminState = new AuthenticationState(principal);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(LocalAdminState);
    }
}

