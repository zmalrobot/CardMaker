namespace CardMaker.Application.Admin;

public interface IDatabaseSnapshotProvider
{
    Task CreateSnapshotAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task<string> CheckIntegrityAsync(CancellationToken cancellationToken = default);
}

