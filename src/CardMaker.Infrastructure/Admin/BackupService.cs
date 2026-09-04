using System.Text.Json;
using CardMaker.Application.Admin;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Admin;

public sealed class BackupService : IBackupService
{
    private readonly CardMakerDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IDatabaseSnapshotProvider _snapshotProvider;
    private readonly ILogger<BackupService> _logger;

    [ActivatorUtilitiesConstructor]
    public BackupService(
        CardMakerDbContext db,
        IConfiguration configuration,
        IDatabaseSnapshotProvider snapshotProvider,
        ILogger<BackupService> logger)
    {
        _db = db;
        _configuration = configuration;
        _snapshotProvider = snapshotProvider;
        _logger = logger;
    }

    public BackupService(
        CardMakerDbContext db,
        IConfiguration configuration,
        ILogger<BackupService> logger)
        : this(db, configuration, new SqliteDatabaseSnapshotProvider(db), logger)
    {
    }

    private string GetBackupsDirectory()
    {
        var dataRoot = _configuration["Storage:DataRoot"] ?? Path.Combine(AppContext.BaseDirectory, "data");
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

        await _snapshotProvider.CreateSnapshotAsync(backupPath, cancellationToken).ConfigureAwait(false);

        var fileInfo = new FileInfo(backupPath);
        var now = DateTimeOffset.UtcNow;

        _db.AuditLog.Add(new AuditLogEntry
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

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Creato snapshot database con successo: {BackupPath} ({SizeBytes} bytes)", backupPath, fileInfo.Length);

        return new BackupFileInfo(fileName, backupPath, fileInfo.Length, now);
    }

    public Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = GetBackupsDirectory();
        return Task.Run<IReadOnlyList<BackupFileInfo>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var files = Directory.EnumerateFiles(backupDir, "cardmaker_backup_*.db")
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Select(f => new BackupFileInfo(
                    f.Name,
                    f.FullName,
                    f.Length,
                    f.CreationTimeUtc))
                .ToList();

            return files;
        }, cancellationToken);
    }

    public async Task<BackupIntegrityReport> VerifyDatabaseIntegrityAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _snapshotProvider.CheckIntegrityAsync(cancellationToken).ConfigureAwait(false);
        var isHealthy = string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("Verifica integrità database eseguita. Risultato: {Result}", result);

        return new BackupIntegrityReport(isHealthy, result, now);
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

        _db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = "DeleteBackup",
            EntityName = "DatabaseBackup",
            EntityId = fileName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
