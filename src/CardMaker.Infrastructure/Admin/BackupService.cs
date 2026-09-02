using System.Data;
using System.Text.Json;
using CardMaker.Application.Admin;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Admin;

public sealed class BackupService(
    CardMakerDbContext db,
    IConfiguration configuration,
    ILogger<BackupService> logger) : IBackupService
{
    private string GetBackupsDirectory()
    {
        var dataRoot = configuration["Storage:DataRoot"] ?? Path.Combine(AppContext.BaseDirectory, "data");
        var backupDir = Path.Combine(dataRoot, "backups");
        Directory.CreateDirectory(backupDir);
        return backupDir;
    }

    public async Task<BackupFileInfo> CreateBackupAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var backupDir = GetBackupsDirectory();
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var fileName = $"cardmaker_backup_{timestamp}.db";
        var backupPath = Path.Combine(backupDir, fileName);

        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        // SQLite VACUUM INTO creates a safe, transactional, consistent snapshot while database is active
        await db.Database.ExecuteSqlAsync($"VACUUM INTO {backupPath};", cancellationToken).ConfigureAwait(false);

        var fileInfo = new FileInfo(backupPath);
        var now = DateTimeOffset.UtcNow;

        db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = "CreateBackup",
            EntityName = "DatabaseBackup",
            EntityId = fileName,
            DetailsJson = JsonSerializer.Serialize(new { fileName, sizeBytes = fileInfo.Length }),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Creato snapshot database SQLite con successo: {BackupPath} ({SizeBytes} bytes)", backupPath, fileInfo.Length);

        return new BackupFileInfo(fileName, backupPath, fileInfo.Length, now);
    }

    public Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = GetBackupsDirectory();
        var files = Directory.GetFiles(backupDir, "cardmaker_backup_*.db")
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new BackupFileInfo(
                f.Name,
                f.FullName,
                f.Length,
                f.CreationTimeUtc))
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupFileInfo>>(files);
    }

    public async Task<BackupIntegrityReport> VerifyDatabaseIntegrityAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
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
            var result = (await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))?.ToString() ?? "unknown";

            var isHealthy = string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);

            logger.LogInformation("Verifica integrità SQLite eseguita. Risultato: {Result}", result);

            return new BackupIntegrityReport(isHealthy, result, now);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> DeleteBackupAsync(string fileName, string? userId, CancellationToken cancellationToken = default)
    {
        var backupDir = GetBackupsDirectory();
        var fullPath = Path.Combine(backupDir, Path.GetFileName(fileName));

        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        var now = DateTimeOffset.UtcNow;

        db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = "DeleteBackup",
            EntityName = "DatabaseBackup",
            EntityId = fileName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
