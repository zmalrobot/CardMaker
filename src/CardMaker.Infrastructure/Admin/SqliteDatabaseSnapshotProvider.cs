using System.Data;
using CardMaker.Application.Admin;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Admin;

public sealed class SqliteDatabaseSnapshotProvider(CardMakerDbContext db) : IDatabaseSnapshotProvider
{
    public async Task CreateSnapshotAsync(string destinationPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        await db.Database.ExecuteSqlAsync($"VACUUM INTO {destinationPath};", cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> CheckIntegrityAsync(CancellationToken cancellationToken = default)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = false;

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            shouldClose = true;
        }

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result?.ToString() ?? "unknown";
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}

