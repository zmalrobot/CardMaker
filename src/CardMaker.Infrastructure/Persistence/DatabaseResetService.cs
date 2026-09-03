using CardMaker.Application.Admin;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Persistence;

/// <summary>
/// Esegue il ripristino di fabbrica del database: chiude i pool di connessione,
/// cancella il file database e riesegue le migrazioni con il seeding iniziale completo.
/// </summary>
public sealed class DatabaseResetService(
    CardMakerDbContext db,
    DatabaseInitializer initializer,
    ILogger<DatabaseResetService> logger) : IDatabaseResetService
{
    public async Task<DatabaseResetResult> ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogWarning("ATTENZIONE: Avvio operazione distruttiva di ripristino di fabbrica del database...");

            // 1. Rilascia tutti i pool di connessione SQLite aperti
            SqliteConnection.ClearAllPools();

            // 2. Elimina il database SQLite e relativi file WAL / SHM
            await db.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);

            // 3. Riesegue migrazioni, seed admin, giochi (YuGiOh, Pokemon, MTG), template e font
            await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Ripristino di fabbrica del database completato con successo.");
            return new DatabaseResetResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Errore irreversibile durante il ripristino di fabbrica del database");
            return new DatabaseResetResult(false, ex.Message);
        }
    }
}
