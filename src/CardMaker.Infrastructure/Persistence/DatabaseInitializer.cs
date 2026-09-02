using System.Security.Cryptography;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Persistence;

/// <summary>
/// Applica le migrazioni e crea i ruoli. Al primo avvio genera l'amministratore iniziale:
/// serve perche' la registrazione libera non esiste, si entra solo su invito (ADR-012).
/// </summary>
public sealed class DatabaseInitializer(
    CardMakerDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await EnableWriteAheadLoggingAsync(cancellationToken).ConfigureAwait(false);

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                await roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
            }
        }

        await EnsureBootstrapAdminAsync().ConfigureAwait(false);
    }

    private async Task EnsureBootstrapAdminAsync()
    {
        var email = configuration["Bootstrap:AdminEmail"];
        if (string.IsNullOrWhiteSpace(email))
        {
            email = "admin@cardmaker.local";
        }

        var configuredPassword = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            configuredPassword = "Admin123!456";
        }

        var admin = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Amministratore",
            };

            var created = await userManager.CreateAsync(admin, configuredPassword).ConfigureAwait(false);
            if (!created.Succeeded)
            {
                logger.LogError(
                    "Creazione dell'amministratore iniziale non riuscita: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(admin, AppRoles.Admin).ConfigureAwait(false);
            logger.LogInformation("Amministratore iniziale creato: {Email}", email);
        }
        else
        {
            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin).ConfigureAwait(false))
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin).ConfigureAwait(false);
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin).ConfigureAwait(false);
            await userManager.ResetPasswordAsync(admin, resetToken, configuredPassword).ConfigureAwait(false);
            logger.LogInformation("Amministratore verificato e sincronizzato: {Email}", email);
        }
    }

    private async Task EnableWriteAheadLoggingAsync(CancellationToken cancellationToken)
    {
        // WAL: indispensabile perche' SQLite regga piu' lettori mentre un utente salva (vedi 02-architecture 9.3).
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken).ConfigureAwait(false);
    }

    private static string GeneratePassword()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return "Aa1!" + Convert.ToBase64String(bytes).Replace('+', 'x').Replace('/', 'y').TrimEnd('=');
    }
}
