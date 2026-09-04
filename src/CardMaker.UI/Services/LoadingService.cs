using CardMaker.Application.Common;

namespace CardMaker.UI.Services;

/// <summary>
/// Implementazione thread-safe di ILoadingService per la gestione dei loader globali in Blazor.
/// </summary>
public sealed class LoadingService : ILoadingService
{
    private int _loadingCount;
    public bool IsLoading => _loadingCount > 0;
    public string Message { get; private set; } = "Caricamento in corso...";

    public event Action? OnChange;

    public void Show(string message = "Caricamento in corso...")
    {
        Interlocked.Increment(ref _loadingCount);
        Message = message;
        NotifyStateChanged();
    }

    public void Hide()
    {
        if (Interlocked.Decrement(ref _loadingCount) <= 0)
        {
            _loadingCount = 0;
            Message = "Caricamento in corso...";
        }
        NotifyStateChanged();
    }

    public IDisposable BeginScope(string message = "Caricamento in corso...")
    {
        Show(message);
        return new LoadingScope(this);
    }

    public async Task<IDisposable> BeginScopeAsync(string message = "Caricamento in corso...")
    {
        Show(message);
        // Cede brevemente l'esecuzione per consentire al renderer Blazor / WebKitGTK di aggiornare il DOM
        // e mostrare l'overlay animato prima che partano operazioni pesanti su CPU o database
        await Task.Delay(35);
        return new LoadingScope(this);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    private sealed class LoadingScope(LoadingService service) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                service.Hide();
            }
        }
    }
}
