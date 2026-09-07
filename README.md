> [!CAUTION]
> ## ⚠️ ATTENZIONE: REPOSITORY DI TEST PER GEMINI 3.8 FLASH ⚠️
>
> **QUESTA REPOSITORY È UTILIZZATA ESCLUSIVAMENTE COME TEST PER LE ABILITÀ E LE CAPACITÀ DI GEMINI 3.8 FLASH.**
>
> - ⚠️ **Stato Sperimentale:** Il codice, l'architettura e la documentazione in questa repository sono generati e modificati per testare le capacità dell'IA.
> - 💥 **Possibili Malfunzionamenti:** Il progetto **potrebbe non funzionare affatto**, presentare crash, comportamenti imprevisti o componenti non operativi.
> - 🚨 **Presenza di Errori Gravi:** Potrebbero essere presenti **bug critici, falle di sicurezza, errori di logica e regressioni gravi**.
> - 🛑 **Nessuna Garanzia:** Non utilizzare questo codice in produzione o per scopi critici senza un'accurata verifica e revisione indipendente.

---

# 🃏 CardMaker

**CardMaker** è una piattaforma professionale *data-driven* per la generazione, composizione, rendering e stampa di carte da gioco collezionabili (TCG).

Il progetto è architettato per supportare nativamente molteplici giochi di carte (TCG standard e giapponesi), con pipeline di rendering tipografico ad altissima precisione basata su **SkiaSharp**, motore di inferenza AI locale multimodale per la generazione di testi e illustrazioni (artwork) basato su modelli **Gemma** e **Stable Diffusion** (formato GGUF), e conformità agli standard tipografici industriali di bleed, trim, safe zone e risoluzione (150 / 300 / 600 DPI).

---

## 🎮 Giochi Supportati Nativamente

| Gioco | Formato Fisico | Master Canvas (600 DPI) | Trim (Taglio) | Bleed | Safe Zone | Font Principali |
|---|---|---|---|---|---|---|
| **Yu-Gi-Oh! & Rush Duel** | Japanese (59 × 86 mm) | 1488 × 2126 px | 1394 × 2031 px | 2.0 mm (47 px) | 3.0 mm (71 px) | Matrix-Bold, Stone Serif, FOT-Rodin Pro M |
| **Pokémon TCG** | Standard Poker (63 × 88 mm) | 1583 × 2173 px | 1488 × 2079 px | 2.0 mm (47 px) | 3.0 mm (71 px) | Gill Sans Bold, Futura Bold |
| **Magic: The Gathering** | Standard Poker (63 × 88 mm) | 1583 × 2173 px | 1488 × 2079 px | 2.0 mm (47 px) | 3.0 mm (71 px) | Beleren Bold, MPlantin Regular & Italic |

---

## 📐 Specifiche Tecniche e Matematiche

### 1. Sistema di Coordinate Full-Bleed
Tutti i frame master coprono il canvas comprensivo dell'abbondanza tipografica (*bleed*):
- **Origine (0, 0)**: Bordo esterno dell'area di abbondanza (Bleed Box).
- **Linea di Taglio (Trim Box)**: Centrata all'interno dell'abbondanza a `+BleedPx` su tutti i lati.
- **Zona di Sicurezza (Safe Zone)**: Margine interno di rispetto per testi e simboli critici a `BleedPx + SafeZonePx`.

### 2. Formule di Conversione Millimetri / Pixel
$$\text{Pixel} = \left\lfloor \frac{\text{Millimetri} \times \text{DPI}}{25.4} + 0.5 \right\rfloor$$

- A 600 DPI: $1\text{ mm} \approx 23.622\text{ px}$
- A 300 DPI: $1\text{ mm} \approx 11.811\text{ px}$
- A 150 DPI: $1\text{ mm} \approx 5.906\text{ px}$

---

## 🤖 Generazione Titolo, Descrizione & Illustrazioni con AI Locale (`CardMaker.AI`)

CardMaker integra un motore di intelligenza artificiale locale e privato, **completamente offline** e senza dipendenze cloud o invio di dati all'esterno, sia per i testi di gioco sia per le illustrazioni delle carte:

### 📝 1. Generazione Testi ed Effetti (LLM)
- 🧠 **Motore llama.cpp nativo**: Basato su `LLamaSharp` (0.27.0) con backend CPU ottimizzato per Windows (`llama.dll`) e Linux (`libllama.so`).
- 🖥️ **Rilevamento Hardware Automatico**: Seleziona automaticamente il modello ottimale in base alla RAM fisica del computer:
  - **4 GB RAM**: Google Gemma 2 2B Instruct (`Q4_K_M`) — compatto e veloce.
  - **8 GB RAM**: Google Gemma 3 4B Instruct (`Q4_K_M`) — bilanciato e raccomandato.
  - **16 GB RAM**: Google Gemma 2 9B Instruct (`Q4_K_M`) — alta fedeltà semantica.
  - **32 GB RAM**: Google Gemma 2 27B Instruct (`Q4_K_M`) — massima creatività e dettaglio.
-  **Stili Narrativi**: Generazione calibrata per contesto di gioco e stile (Classico, Epico/Mitologico, Oscuro/Gotico, Umoristico/Satirico, Tattico/Tecnico).

### 🎨 2. Generazione Illustrazioni ed Artwork (Diffusione)
- 🖼️ **Motore Stable Diffusion GGUF**: Generazione locale di illustrazioni originali text-to-image a 512×512 e 768×768 px.
- 🗃️ **Profili Modelli Immagine Preconfigurati**:
  - **Stable Diffusion 1.5 Turbo (Q8_0, 2.02 GB)**: Ottimizzato per macchine a basse risorse (≥ 4 GB RAM) ed esecuzione fulminea in 1–4 step.
  - **DreamShaper 8 LCM (IQ4_NL, 1.57 GB)**: Consigliato per configurazioni standard (≥ 8 GB RAM), perfetto per stili TCG fantasy e creature in 4–8 step.
  - **SDXL Lightning 4-Step (Q4_0, 2.58 GB)**: Elevatissima qualità artistica e risoluzione nativa 768×768 px per macchine potenti (≥ 16 GB RAM).
- 🎨 **Preset di Stile Artistico**: Supporto immediato per molteplici estetiche: *Fantasy / Card Game Illustrativo*, *Anime / Manga TCG*, *Dark Fantasy & Horror*, *Sci-Fi / Cyberpunk*, *Dipinto ad Olio Classico*, *Acquerello & Inchiostro Giapponese*.
- 💾 **Salvataggio Diretto nel Catalogo Asset**: L'immagine generata e accettata dall'utente viene automaticamente catalogata nello storage dei contenuti dell'utente (`IAssetCatalog` / SHA-256) e assegnata immediatamente come artwork della carta nell'editor.

### ⚡ 3. Gestione e Infrastruttura Condivisa
- ⚡ **Download Automatico & HTTP Range Resume**: All'avvio dell'applicazione, sia il modello testuale sia il modello di diffusione vengono verificati e scaricati in background senza bloccare la UI, con ripresa automatica dei byte parziali (`Range: bytes=...`) e validazione header GGUF.
- 🔒 **Lifecycle & Zero RAM Waste**: Nessun consumo di memoria RAM a riposo: il caricamento dei pesi avviene on-demand quando si apre il rispettivo modal nell'editor e viene deallocato determinatamente alla chiusura.
- 🏷️ **Monitoraggio in Tempo Reale**: Il footer applicativo e il banner in testata mostrano stato, velocità (MB/s), ETA e modello attivo per testo e immagini.

---

## 🏗️ Architettura della Solution

Il progetto adotta un'architettura modulare a livelli conforme ai principi della *Clean / Hexagonal Architecture*, compilata con **.NET 10** (C# 13):

```text
CardMaker.slnx
├── src/CardMaker.Domain           # Entità del dominio, aggregati (Card, Template, Asset, Game), Identity e Audit
├── src/CardMaker.Contracts        # DTO, geometrie (CardGeometry), AST condizionale (ConditionOps) e layout JSON
├── src/CardMaker.Application      # Interfacce di servizio (Porte), validatori, logica applicativa, seeder e AI Manager
├── src/CardMaker.AI               # Libreria AI: binding llama.cpp, registry Gemma/SD GGUF, engine testo e immagini
├── src/CardMaker.Rendering        # Motore SkiaSharp: rasterizzatore, TextEngine (auto-fit), simboli procedurali, PDF
├── src/CardMaker.Infrastructure   # Implementazioni: EF Core SQLite, IAssetStore, FontCatalog, AI Downloader resiliente
├── src/CardMaker.UI               # Razor Class Library (RCL): componenti grafici, editor dinamici, AI modal, banner, Admin
├── src/CardMaker.Desktop          # Host nativo multipiattaforma basato su Photino.Blazor (Linux, Windows, macOS)
├── src/CardMaker.Web              # Host Web ASP.NET Core: middleware di sicurezza, rate-limiting, healthcheck
└── tests/                         # Suite automatica: 256 test di unità, integrazione, AI e rendering (100% verdi)
```

---

## ✨ Funzionalità Principali

- 🎴 **Supporto Multi-Gioco Flessibile**: Gestione simultanea di giochi diversi con geometrie, frame e formati di testo dedicati (Yu-Gi-Oh!, Pokémon TCG, Magic: The Gathering).
- 🤖 **Assistente Creativo AI Multimodale**:
  - Generazione assistita di nomi, abilità, regole e descrizioni TCG con modelli **Google Gemma** via `llama.cpp`.
  - Generazione automatica di illustrazioni originali (artwork) in-app con **Stable Diffusion GGUF**, stili artistici dedicati (fantasy, manga, cyberpunk, pittura) e catalogazione immediata negli asset di gioco.
- ⚡ **Motore di Rendering Dati-Driven (SkiaSharp)**:
  - Generazione di output raster **PNG**, **JPEG** e vettoriali **PDF**.
  - Risoluzioni calibrate: Anteprima rapida a **150 DPI**, stampa ad alta definizione a **300 DPI** e **600 DPI**.
  - Pipeline tipografica con auto-fit intelligente (*shrink*, *condense*, *shrink-and-condense*), centraggio ottico calibrato su `CapHeight` e simboli inline `{sym:...}`.
- 🎨 **Template Studio WYSIWYG (`/admin/templates/{id}`)**:
  - Editor interattivo a 3 pannelli per la progettazione grafica dei template.
  - Albero dei layer polimorfi con ordinamento Z-order e condizioni visive dinamiche (`VisibleWhen`).
  - Guide a video per **Bleed** (abbondanza 2 mm) e **Safe Zone** (zona di sicurezza 3 mm).
- 🧙 **Wizard e Form Dinamici**:
  - Creazione guidata delle carte con selezione del gioco e del tipo.
  - Form generato automaticamente in base ai metadati dei campi (`FieldDefinition`), con campi condizionali e selettore di tratti.
  - Anteprima live debouncata (200 ms) a 60 FPS con offload in background (`Task.Run`).
- 🖥️ **Doppia Modalità: Web & Desktop**:
  - **Desktop (Photino.Blazor)**: Eseguibile nativo leggero cross-platform (Linux WebKitGTK, Windows WebView2, macOS WebKit) con storage locale e bypass amministratore offline automatico.
  - **Web (ASP.NET Core)**: Modalità multi-utente con registrazione a invito, protezione rate limiting, Content Security Policy restrittiva e snapshot SQLite online (`VACUUM INTO`).
- 🔕 **Logging Strutturato & Pulito**:
  - Eliminazione totale del rumore di dump IPC Base64 in console (`SetLogVerbosity(0)`).
  - Log sintetici ad alta leggibilità per anteprima, export, gestione carte, AI e caricamento asset.

---

## 🚀 Avvio Rapido

### Prerequisiti
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Script di Avvio Rapido (Clean + Restore + Build + Run)
A livello di root del repository sono presenti comodi script con gestione automatica di pulizia e restore:

```bash
# Linux / macOS
./run-desktop.sh    # Avvia l'applicazione Desktop nativa (Photino.Blazor)
./run-web.sh        # Avvia l'applicazione Web (Kestrel su http://localhost:5240)
```

```cmd
:: Windows
run-desktop.bat     :: Avvia l'applicazione Desktop nativa
run-web.bat         :: Avvia l'applicazione Web (http://localhost:5240)
```

---

## 🔑 Credenziali Amministratore Predefinite

Al primo avvio, il database SQLite locale viene inizializzato e popolato con i contenuti base e un utente amministratore:
- **Email**: `admin@cardmaker.local`
- **Password**: `Admin123!456`

*(In modalità Desktop, l'accesso amministrativo offline è automatico e non richiede login)*.

---

## 🧪 Collaudo e Suite di Test

Per eseguire l'intera suite di collaudo automatizzata:
```bash
dotnet test
```

Attualmente la suite include **256 test** (104 test di rendering/geometria e 152 test applicativi/integrazione/AI), tutti superati con 0 errori e 0 avvisi.

---

## 📦 CI/CD e Distribuzione Multi-Piattaforma

Il progetto include workflow nativi al 100% per **GitHub Actions**:

- ⚡ **Fast CI (`.github/workflows/ci-fast.yml`)**: Eseguito ad ogni `push` e `pull_request`. Esegue il restore con cache, la compilazione in Release con `TreatWarningsAsErrors=true`, 231 test essenziali (con esclusione dei golden test di rendering locale) e l'audit dei pacchetti NuGet vulnerabili.
- 🚀 **Release Pipeline (`.github/workflows/release.yml`)**: Eseguito al push di un tag di versione (`v*`) o tramite avvio manuale (`workflow_dispatch`). Compila ed esporta binari ottimizzati per Windows e Linux, genera i checksum SHA-256 (`SHA256SUMS.txt`) e pubblica automaticamente la GitHub Release:
  - **Windows (x64)**:
    - `CardMaker-<tag>-Windows-Setup-x64.exe` — Installer desktop guidato (compilato con Inno Setup).
    - `CardMaker-<tag>-Windows-Portable-x64.zip` — Portatile Self-Contained (zero prerequisiti, runtime .NET 10 incluso).
    - `CardMaker-<tag>-Windows-FDD-x64.zip` — Portatile Framework-Dependent (~15 MB, per chi ha già .NET 10 installato).
  - **Linux (x64)**:
    - `CardMaker-<tag>-Linux-amd64.deb` — Pacchetto nativo Debian/Ubuntu/Mint con integrazione FreeDesktop (icone e `.desktop`).
    - `CardMaker-<tag>-Linux-x64.tar.gz` — Tarball portatile universale Self-Contained con script `run.sh`.
    - `CardMaker-<tag>-Web-Standalone.zip` — Archivio standalone per l'host Web Kestrel.

---

## 📚 Documentazione di Progetto

Nella cartella [`docs/`](docs/) è disponibile la knowledge base tecnica completa per sviluppatori e grafici:

- [`docs/README.md`](docs/README.md): Indice generale della documentazione.
- [`docs/00-overview/project-context.md`](docs/00-overview/project-context.md): Master context document — punto di partenza per nuove sessioni.
- [`docs/01-architecture/ai-engine.md`](docs/01-architecture/ai-engine.md): Architettura del motore AI locale (llama.cpp, profili Gemma, startup download e resume).
- [`docs/01-architecture/architecture.md`](docs/01-architecture/architecture.md): Architettura, pipeline di rendering e multi-host.
- [`docs/03-data/data-model.md`](docs/03-data/data-model.md): Modello dati relazionale e schema JSON dei template.
- [`docs/09-decisions/README.md`](docs/09-decisions/README.md): Registro delle decisioni architetturali (ADR-001 → ADR-038).
- [`docs/10-reference/asset-spec.md`](docs/10-reference/asset-spec.md): Specifiche dimensionali per grafici e asset.
- [`docs/02-development/dev-guide.md`](docs/02-development/dev-guide.md): Guida per sviluppatori, configurazione e rotte applicative.
- [`docs/02-development/resume-prompt.md`](docs/02-development/resume-prompt.md): Prompt di ripristino contesto rapido per nuove sessioni AI.
