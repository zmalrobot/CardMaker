namespace CardMaker.Application.Admin;

public sealed record BackupFileInfo(
    string FileName,
    string FilePath,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record BackupIntegrityReport(
    bool IsHealthy,
    string CheckResult,
    DateTimeOffset CheckedAtUtc);

public interface IBackupService
{
    Task<BackupFileInfo> CreateBackupAsync(string? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupFileInfo>> ListBackupsAsync(CancellationToken cancellationToken = default);
    Task<BackupIntegrityReport> VerifyDatabaseIntegrityAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteBackupAsync(string fileName, string? userId, CancellationToken cancellationToken = default);
}

