namespace CardMaker.Application.Content;

public sealed record GameImportResult(bool Succeeded, string? ErrorCode, string? GameKey);

/// <summary>
/// Esporta/importa un gioco intero (tipi di carta, campi, template pubblicati, set di simboli,
/// liste opzioni, traits e gli asset binari eventualmente gia' caricati) in un pacchetto
/// <c>.cmpkg</c> (zip). Serve a spostare un gioco fra installazioni, non a distribuire asset con
/// l'applicazione (resta valida ADR-010: il pacchetto lo produce l'admin, non lo spedisce il repo).
/// </summary>
public interface IGamePackageService
{
    Task<byte[]> ExportAsync(string gameKey, CancellationToken cancellationToken = default);

    Task<GameImportResult> ImportAsync(Stream cmpkgStream, CancellationToken cancellationToken = default);
}
