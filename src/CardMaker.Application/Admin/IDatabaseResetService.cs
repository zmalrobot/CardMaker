namespace CardMaker.Application.Admin;

public sealed record DatabaseResetResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Servizio per il ripristino distruttivo di fabbrica del database SQLite.
/// </summary>
public interface IDatabaseResetService
{
    Task<DatabaseResetResult> ResetDatabaseAsync(CancellationToken cancellationToken = default);
}
