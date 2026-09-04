namespace CardMaker.Application.Common;

/// <summary>
/// Servizio per la gestione centralizzata delle schermate di caricamento e operazioni lunghe.
/// </summary>
public interface ILoadingService
{
    bool IsLoading { get; }
    string Message { get; }
    event Action? OnChange;

    void Show(string message = "Caricamento in corso...");
    void Hide();
    IDisposable BeginScope(string message = "Caricamento in corso...");
    Task<IDisposable> BeginScopeAsync(string message = "Caricamento in corso...");
}
