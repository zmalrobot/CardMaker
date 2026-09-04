# Mappa dei Progetti della Solution

La solution `CardMaker.slnx` è organizzata in 7 progetti applicativi e 2 progetti di collaudo:

```text
CardMaker.slnx
├── src/
│   ├── CardMaker.Domain/
│   ├── CardMaker.Contracts/
│   ├── CardMaker.Application/
│   ├── CardMaker.Rendering/
│   ├── CardMaker.Infrastructure/
│   ├── CardMaker.UI/
│   ├── CardMaker.Desktop/
│   └── CardMaker.Web/
└── tests/
    ├── CardMaker.Rendering.Tests/
    └── CardMaker.Application.Tests/
```

---

## 1. `CardMaker.Domain`
* **Tipo**: Class Library (`Microsoft.NET.Sdk`)
* **Dipendenze**: Nessuna dipendenza da altri progetti della solution.
* **Ruolo**: Costituisce il cuore del dominio applicativo.
* **Contenuti Principali**:
  - `Game`: entità radice che definisce le regole fisiche del gioco (dimensioni mm, angoli, abbondanza).
  - `CardType`: tipologia di carta (es. Mostro Normale, Fase 1, Creatura) legata a uno schema campi.
  - `Card`: aggregato della carta creata dall'utente con titolo, valori e tratti selezionati.
  - `CardTemplate` e `CardTemplateVersion`: modello immutabile dei layout grafici.
  - `Asset` e `FontAsset`: metadati delle risorse grafiche e dei font tipografici.
  - `Invitation`: token crittografici SHA-256 monouso per la registrazione controllata.
  - `AuditLogEntry`: registro immutabile degli eventi applicativi.

---

## 2. `CardMaker.Contracts`
* **Tipo**: Class Library (`Microsoft.NET.Sdk`)
* **Dipendenze**: `CardMaker.Domain`.
* **Ruolo**: Modelli di scambio, contratti geometrici e definizione astratta dei layout.
* **Contenuti Principali**:
  - `CardGeometry`: formule e calcoli per la conversione millimetri/pixel a 150, 300 e 600 DPI, aree di trim, bleed e safe zone.
  - `CardTemplateLayout`: schema JSON tipizzato dei layer (`ImageLayer`, `TextLayer`, `RichTextLayer`, `SymbolRepeaterLayer`, `LinkArrowsLayer`, `FoilLayer`).
  - `ConditionOps` e `ConditionGroup`: AST tipizzato per le regole visive condizionali (`VisibleWhen`).
  - `ValueBinder`: motore deterministico per la risoluzione delle interpolazioni `{{campo}}`.
  - `ConditionEvaluator`: interprete delle condizioni logiche sui campi della carta.

---

## 3. `CardMaker.Application`
* **Tipo**: Class Library (`Microsoft.NET.Sdk`)
* **Dipendenze**: `CardMaker.Domain`, `CardMaker.Contracts`.
* **Ruolo**: Logica applicativa, porte di servizio (*Port interfaces*) e validazione.
* **Contenuti Principali**:
  - Porte di servizio: `ICardService`, `ICardExportService`, `ICardPreviewService`, `IAssetCatalog`, `IFontCatalog`, `IAdminContentService`, `IInvitationService`, `IBackupService`.
  - Servizi di calcolo: `CardDerivedValuesService` (calcolo automatico statistiche e formattazioni di testo specifiche per gioco).
  - Validazione: `UploadValidator` (filtro estensioni consentite, magic bytes immagini e blocco font non conformi come `.woff2`).
  - Interfacce di astrazione: `IAssetStore`, `IDatabaseSnapshotProvider`, `IFileDownloadService`.

---

## 4. `CardMaker.Rendering`
* **Tipo**: Class Library (`Microsoft.NET.Sdk`)
* **Dipendenze**: `CardMaker.Contracts`.
* **Ruolo**: Motore grafico puro SkiaSharp indipendente dal database e dal web framework.
* **Contenuti Principali**:
  - `CardRenderer`: orchestratore del disegno su superfici Skia raster o PDF.
  - Strategy Painters (`Painters/`): `ImageLayerPainter`, `TextLayerPainter`, `RichTextLayerPainter`, `SymbolRepeaterLayerPainter`, `LinkArrowsLayerPainter`, `FoilLayerPainter`.
  - `TextEngine`: calcolo avanzato dell'auto-fit (*shrink* e *condense*) con centraggio ottico calibrato su `CapHeight`.
  - `FontRegistry`: gestione thread-safe dei `SKTypeface` e mapping degli alias tipografici.
  - Generatori procedurali: creazione geometrica vettoriale di frame segnaposto, energie Pokémon e simboli di mana MTG.
  - `PdfExporter`: generazione di documenti PDF conformi a 600 DPI con `SKDocument`.

---

## 5. `CardMaker.Infrastructure`
* **Tipo**: Class Library (`Microsoft.NET.Sdk`)
* **Dipendenze**: `CardMaker.Domain`, `CardMaker.Contracts`, `CardMaker.Application`, `CardMaker.Rendering`.
* **Ruolo**: Implementazione concreta dei dettagli infrastrutturali.
* **Contenuti Principali**:
  - Persistenza: `CardMakerDbContext` (EF Core SQLite), configurazioni fluent, migrazioni in `Persistence/Migrations/`.
  - Storage: `FileSystemAssetStore` (storage content-addressed SHA-256), `FontService`.
  - Seeding: `DatabaseInitializer`, `ContentGraphSeeder`, seeder specifici per Yu-Gi-Oh!, Pokémon e Magic con font TTF/OTF incorporati come risorse.
  - Snapshot DB: `SqliteDatabaseSnapshotProvider` con esecuzione di `VACUUM INTO` e `PRAGMA integrity_check`.

---

## 6. `CardMaker.UI`
* **Tipo**: Razor Class Library (`Microsoft.NET.Sdk.Razor`)
* **Dipendenze**: `CardMaker.Application`, `CardMaker.Contracts`, `CardMaker.Domain`.
* **Ruolo**: Componenti visuali, pagine Blazor e risorse statiche web (CSS, JS, branding).
* **Contenuti Principali**:
  - Pagine Utente: `/cards` (collezione), `/cards/create` (wizard), `/cards/edit/{id}` (editor dinamico con anteprima 60 FPS), `/guida`, `/disclaimer`.
  - Pagine Admin: `/admin/content`, `/admin/schema/{id}`, `/admin/templates/{id}` (Template Studio), `/admin/assets`, `/admin/fonts`, `/admin/placeholders`, `/admin/audit`, `/admin/invitations`, `/admin/backups`.
  - Componenti condivisi: `CardPreview`, `AssetImage`, `FontPreviewImage`, `DynamicCardForm`, `ExportModal`, `ThemeToggle`, `SkeletonLoader`.
  - Design System: `wwwroot/css/cardmaker-theme.css`, `wwwroot/js/theme.js`.

---

## 7. `CardMaker.Desktop`
* **Tipo**: Eseguibile Desktop (`Microsoft.NET.Sdk.Razor`, `WinExe`)
* **Dipendenze**: Tutte le librerie della solution, con inclusione di `Photino.Blazor`.
* **Ruolo**: Host nativo multipiattaforma (Linux, Windows, macOS).
* **Contenuti Principali**:
  - `Program.cs`: punto di ingresso, configurazione della finestra Photino nativa (`SetTitle`, `SetIconFile`, `SetSize`).
  - `DesktopPathResolver`: risoluzione automatica delle cartelle utente di sistema (`.local/share`, `%LOCALAPPDATA%`).
  - `DesktopAuthenticationStateProvider`: bypass dell'autenticazione offline con ruolo `Admin` predefinito.
  - `DesktopAssetUriService`: risoluzione in-memory di data URI Base64 con cache `IMemoryCache`.
  - `DesktopFileDownloadService`: salvataggio file nativo con integrazione dialoghi OS (`zenity`, `kdialog`, Photino).

---

## 8. `CardMaker.Web`
* **Tipo**: Web App (`Microsoft.NET.Sdk.Web`)
* **Dipendenze**: `CardMaker.Infrastructure`, `CardMaker.UI`.
* **Ruolo**: Host ASP.NET Core per pubblicazione su server o container Docker.
* **Contenuti Principali**:
  - `Program.cs`: configurazione Kestrel, pipeline HTTP, Identity cookie, rate limiting sliding window.
  - Middleware di sicurezza: Content Security Policy restrittiva, X-Frame-Options, X-Content-Type-Options.
  - Endpoint `/healthz` per health check di sistema.
  - `WebAssetUriService`: controller e mapping per la fruizione degli asset via HTTP.
