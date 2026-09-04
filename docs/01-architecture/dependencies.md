# Dipendenze Esterne e Requisiti di Sistema

La solution minimizza le dipendenze esterne verso librerie di terze parti, privilegiando il framework base di **.NET 10** e standard open-source stabili.

---

## 1. Pacchetti NuGet Principali

| Pacchetto | Versione | Progetti Utilizzatori | Scopo / Motivazione |
|---|---|---|---|
| `SkiaSharp` | 4.151.1 | `CardMaker.Rendering`, `CardMaker.Infrastructure` | Motore di disegno 2D raster/vettoriale. Gestisce blend modes, matrici di trasformazione, path ed export PDF. |
| `SkiaSharp.HarfBuzz` | 4.151.1 | `CardMaker.Rendering` | Shaping tipografico complesso, misurazione precisa dell'avanzamento dei glifi e baseline. |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.11 | `CardMaker.Infrastructure`, `CardMaker.Application.Tests` | Provider SQLite per EF Core 10. Supporta WAL mode, migrazioni e pooling. |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 | `CardMaker.Infrastructure`, `CardMaker.Desktop`, `CardMaker.Web` | Gestione utenti, ruoli `Admin`/`User`, password hashing e token Identity. |
| `Photino.Blazor` | 4.0.13 | `CardMaker.Desktop` | Shell nativa desktop multipiattaforma. Incapsula la WebView di sistema nativa senza runtime Chromium embedded pesanti (Electron). |
| `coverlet.collector` | 6.0.4 | `tests/*` | Raccolta e reportistica della code coverage dei test automatizzati. |
| `xunit` / `xunit.runner.visualstudio` | 2.9.3 / 3.1.4 | `tests/*` | Framework di esecuzione test e integrazione CLI `dotnet test`. |

---

## 2. Requisiti di Sistema e Librerie Native

### Linux (Ubuntu, Debian, Fedora, Arch)
Per l'esecuzione dell'host Desktop nativo (`CardMaker.Desktop`), il sistema operativo deve disporre delle librerie grafiche GTK e WebKit:
- `libwebkit2gtk-4.1-0` (o `webkit2gtk4.1` su Fedora/RHEL)
- `libfontconfig1`
- `libfreetype6`

Su distribuzioni Debian/Ubuntu:
```bash
sudo apt-get update && sudo apt-get install -y libwebkit2gtk-4.1-0 libfontconfig1 libfreetype6
```

### Windows
- Windows 10 (versione 1809 o successiva) o Windows 11.
- **Microsoft Edge WebView2 Runtime** (generalmente preinstallato di sistema).

### macOS
- macOS 11.0 (Big Sur) o versioni successive (supporto nativo sia per architetture Intel x64 che Apple Silicon ARM64).
- Runtime WebKit nativo integrato in macOS.

---

## 3. Tool e Strumenti di Compilazione

- **.NET SDK 10**: Versione minima consigliata 10.0.100 o superiore.
- **`dotnet-ef`**: Configurato come tool locale in `.config/dotnet-tools.json` per la gestione uniforme delle migrazioni tra macchine.
- **Python 3**: Utilizzato facoltativamente per l'esecuzione dello script di generazione deterministica degli asset di branding (`scripts/generate-brand-assets.py`).
