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
    CardMaker.Application.Assets.IPlaceholderSeeder placeholderSeeder,
    CardMaker.Application.Content.IYuGiOhContentSeeder yugiohSeeder,
    CardMaker.Application.Assets.IYuGiOhFontSeeder fontSeeder,
    CardMaker.Application.Content.IPokemonContentSeeder pokemonSeeder,
    CardMaker.Application.Assets.IPokemonFontSeeder pokemonFontSeeder,
    CardMaker.Application.Content.IMtgContentSeeder mtgSeeder,
    CardMaker.Application.Assets.IMtgFontSeeder mtgFontSeeder,
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

        // Assicura che i template, i frame e i font di default di Yu-Gi-Oh, Pokémon e Magic siano sempre presenti al primo avvio
        try
        {
            await placeholderSeeder.SeedYuGiOhAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await yugiohSeeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            await fontSeeder.SeedDefaultFontsAsync(cancellationToken).ConfigureAwait(false);

            await pokemonSeeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            await placeholderSeeder.SeedPokemonAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await pokemonFontSeeder.SeedDefaultFontsAsync(cancellationToken).ConfigureAwait(false);

            await mtgSeeder.SeedAsync(cancellationToken).ConfigureAwait(false);
            await placeholderSeeder.SeedMtgAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await mtgFontSeeder.SeedDefaultFontsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Errore durante il seeding iniziale dei template e dei font di gioco");
        }
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
