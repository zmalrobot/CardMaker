# Code Optimization Analysis

> **Ruolo**: Principal .NET Architect / Senior C# Engineer  
> **Target Solution**: CardMaker (.NET 10, Blazor Web + Blazor Hybrid Desktop, EF Core, SkiaSharp)  
> **Modalità**: **SOLO ANALISI (ANALYSIS ONLY)** — Nessun file sorgente modificato, nessuna variazione della logica funzionale.  
> **Data**: 2026-09-04  

---

## 1. Executive Summary

La solution **CardMaker** implementa un card maker multi-gioco (Yu-Gi-Oh!, Magic: The Gathering, Pokémon) cross-platform (.NET 10) con rendering su base grafica SkiaSharp, persistenza su SQLite via Entity Framework Core, e due frontend grafici:
1. **Web**: Blazor Server / WebApp interattiva con Identity e ruoli (Admin/User).
2. **Desktop**: Blazor Hybrid tramite Photino.NET (`CardMaker.Desktop`), eseguito in-process senza server web.

### Stato Attuale della Codebase
- **Punti di Forza**:
  - Netta separazione concettuale fra specifiche grafiche astratte (`CardMaker.Contracts`), motore di composizione (`CardMaker.Rendering`) e layout data-driven (ADR-001).
  - Suite di test solida (155 unit & integration test in `CardMaker.Application.Tests` e `CardMaker.Rendering.Tests`, tutti attualmente verdi).
  - Meccanismo di caching a due livelli (`DecodedImageCache` basato su LRU + `PreloadedRenderResources`) per limitare il decoding SkiaSharp.
- **Criticità e Aree di Ottimizzazione Rilevate**:
  - **Inversione delle dipendenze violata (Clean Architecture)**: `CardMaker.Application` dipende direttamente da `CardMaker.Rendering` solo per usare due classi di valutazione logica (`ConditionEvaluator` e `ValueBinder`), forzando l'intero layer applicativo a referenziare le librerie grafiche native SkiaSharp.
  - **Duplicazione sistematica di logica di Seeding**: Tre seeder di font (`YuGiOhFontSeeder`, `PokemonFontSeeder`, `MtgFontSeeder`) e tre seeder di contenuti (`YuGiOhContentSeeder`, `PokemonContentSeeder`, `MtgContentSeeder`) condividono oltre il 90% del codice algoritmico di aggiornamento grafi e registrazione stream.
  - **Collo di bottiglia N+1 query**: In `PlaceholderSeeder.cs`, ad ogni iterazione di generazione frame e simbolo viene invocata la query `catalog.ListAsync(500)`, moltiplicando inutilmente le letture a database durante il bootstrap o la rigenerazione.
  - **Classi monolitiche (God Objects)**: `CardRenderer.cs` (1005 righe) gestisce contemporaneamente 9 tipologie di layer, post-processing geometrico, gestione DPI e trasformazioni di colore; `ContentManager.razor` (1280 righe) accentra l'intero pannello amministrativo per 5 domini diversi.
  - **Perdita del principio data-driven nella UI**: In `CardEditor.razor` (linee 336–435) sono cablate regole di formattazione testuale specifiche per gioco (`UpdateYuGiOhValues`, `UpdateMtgValues`, `UpdatePokemonValues`), violando la regola architetturale ADR-001 che vieta branch C# hardcoded per singolo gioco.
  - **Accoppiamento rigido al provider SQLite**: `BackupService.cs` invoca comandi raw specifici SQLite (`VACUUM INTO` e `PRAGMA integrity_check`) e manipola direttamente `System.IO.File` / `System.IO.Directory`.

Tutte le proposte descritte in questo documento sono categorizzate rigorosamente in:
- **Problema certo**: Difetto architetturale, bug potenziale o collo di bottiglia comprovato.
- **Possibile miglioramento**: Refactoring evolutivo per manutenibilità, leggibilità o modularità.
- **Ipotesi**: Soluzione alternativa da valutare in base a scenari d'uso futuri.
- **REQUIRES REVIEW**: Modifica che presenta rischi di concorrenza, breaking changes o impatto su contratti pubblici e richiede validazione manuale preventiva.

---

## 2. Current Architecture

La solution è organizzata secondo un pattern a strati con separazione tra contratti, dominio, applicazione, infrastruttura, motore grafico e presentazioni.

```
                    ┌─────────────────────────┐
                    │    CardMaker.Web        │ (ASP.NET Core / Identity)
                    └───────────┬─────────────┘
                                │
                                ▼
                    ┌─────────────────────────┐
                    │    CardMaker.Desktop    │ (Photino.Blazor / Local Bypass)
                    └───────────┬─────────────┘
                                │
                                ▼
                    ┌─────────────────────────┐
                    │     CardMaker.UI        │ (Razor Class Library condivisa)
                    └───────────┬─────────────┘
                                │
        ┌───────────────────────┼────────────────────────┐
        ▼                       ▼                        ▼
┌──────────────────┐  ┌──────────────────┐    ┌────────────────────┐
│CardMaker.Applicat│  │CardMaker.Contract│    │CardMaker.Rendering │
└───────┬──────────┘  └──────────────────┘    └────────────────────┘
        │                       ▲                        ▲
        ▼                       │                        │
┌──────────────────┐            │                        │
│CardMaker.Domain  │────────────┴────────────────────────┘
└──────────────────┘
        ▲
        │
┌──────────────────┐
│CardMaker.Infrastr│ (EF Core SQLite, AssetStore, Seeder, Backup)
└──────────────────┘
```

### Elenco e Responsabilità dei Progetti

| Progetto | Tipo | Responsabilità Principale |
| :--- | :--- | :--- |
| **`CardMaker.Contracts`** | Class Library | DTO, definizioni JSON dei layer (`CardLayout`, `LayerDefinition`), geometrie (`NormalizedRect`), modelli di stile (`TextStyle`). Indipendente da SkiaSharp e dal DB. |
| **`CardMaker.Domain`** | Class Library | Entità di dominio EF Core (`Card`, `Game`, `CardType`, `Trait`, `SymbolSet`, `Asset`, `FontAsset`, `Template`). Logica pura di business ed entità di persistenza. |
| **`CardMaker.Application`** | Class Library | Interfacce dei servizi (`ICardService`, `IAssetCatalog`, `ITemplateSelector`), casi d'uso applicativi, validatori di upload. *(Anomalia: dipende da `CardMaker.Rendering`)*. |
| **`CardMaker.Rendering`** | Class Library | Motore di rendering 2D basato su SkiaSharp e HarfBuzz. Valutazione visibilità, rendering layer, generazione PDF e font registry. |
| **`CardMaker.Infrastructure`** | Class Library | Implementazioni concrete: `CardMakerDbContext` (SQLite), `FileSystemAssetStore`, `CardPreviewService`, `CardExportService`, `BackupService`, Seeders. |
| **`CardMaker.UI`** | Razor Class Library | Componenti Blazor riutilizzabili: `CardEditor`, `DynamicCardForm`, `ContentManager`, `TemplateEditor`, `GlobalLoadingOverlay`, stili CSS comuni. |
| **`CardMaker.Desktop`** | Photino Blazor | Entry point desktop offline/in-process. Mock authentication, bypass permessi admin, asset URI locali. |
| **`CardMaker.Web`** | ASP.NET Core WebApp | Entry point web server. Endpoint asset HTTP, autenticazione ASP.NET Identity con cookie/passkey, layout web. |
| **`CardMaker.Application.Tests`** | xUnit | Test di unità per servizi applicativi e integrazione database SQLite in-memory. |
| **`CardMaker.Rendering.Tests`** | xUnit | Test di regressione visiva, rendering pipeline, binding e serializzazione layout. |

---

## 3. Dependency Map

### Mappa delle Dipendenze Attuali tra Progetti

```
CardMaker.Contracts
  └─ (Nessuna dipendenza di progetto)

CardMaker.Domain
  └─ (Nessuna dipendenza di progetto)

CardMaker.Rendering
  └─ CardMaker.Contracts

CardMaker.Application  <-- [ANOMALIA: VIOLAZIONE CLEAN ARCHITECTURE]
  ├─ CardMaker.Domain
  ├─ CardMaker.Contracts
  └─ CardMaker.Rendering  <-- Dovuta unicamente a TemplateSelector -> ValueBinder/ConditionEvaluator

CardMaker.Infrastructure
  ├─ CardMaker.Domain
  ├─ CardMaker.Contracts
  ├─ CardMaker.Application
  └─ CardMaker.Rendering

CardMaker.UI
  ├─ CardMaker.Application
  └─ CardMaker.Contracts

CardMaker.Desktop
  ├─ CardMaker.Domain
  ├─ CardMaker.Contracts
  ├─ CardMaker.Application
  ├─ CardMaker.Rendering
  ├─ CardMaker.Infrastructure
  └─ CardMaker.UI

CardMaker.Web
  ├─ CardMaker.Application
  ├─ CardMaker.Infrastructure
  ├─ CardMaker.Rendering
  └─ CardMaker.UI
```

### Analisi delle Anomalie Architetturali

1. **`ARCH-001`: Inversione della Dipendenza tra Application e Rendering**  
   - *Categoria*: Problema certo  
   - *Dettaglio*: In Clean Architecture, l'Application layer non deve dipendere da librerie di infrastruttura o presentazione come il motore grafico SkiaSharp. Attualmente `CardMaker.Application.csproj` include una `<ProjectReference>` a `CardMaker.Rendering`.
   - *Causa radice*: `TemplateSelector.cs` usa `ValueBinder` e `ConditionEvaluator` che risiedono nel namespace `CardMaker.Rendering.Pipeline`.
   - *Risoluzione*: Spostare `ValueBinder` e `ConditionEvaluator` (che non usano SkiaSharp ma solo `CardValue` e `Condition` di `CardMaker.Contracts`) in `CardMaker.Contracts` oppure in un modulo leggero `CardMaker.Expressions` o all'interno di `CardMaker.Application`. In questo modo `CardMaker.Application` non referenzierà più `CardMaker.Rendering`.

2. **`ARCH-002`: Doppio Riferimento di Progetti nei Frontend**  
   - *Categoria*: Possibile miglioramento  
   - *Dettaglio*: Sia `CardMaker.Desktop` che `CardMaker.Web` referenziano individualmente quasi tutti i progetti (`Domain`, `Contracts`, `Application`, `Rendering`, `Infrastructure`, `UI`). `CardMaker.UI` dovrebbe fungere da facciata primaria per i componenti grafici, mentre la composizione delle dipendenze (IoC container) nei Program.cs dovrebbe basarsi su metodi di estensione standard (`AddApplication()`, `AddInfrastructure()`, `AddRendering()`).

---

## 4. Duplicate Code (DUP-xxx)

### `DUP-001`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File Coinvolti**:
  - `src/CardMaker.Infrastructure/Storage/YuGiOhFontSeeder.cs`
  - `src/CardMaker.Infrastructure/Storage/PokemonFontSeeder.cs`
  - `src/CardMaker.Infrastructure/Storage/MtgFontSeeder.cs`
- **Metodi**: `SeedDefaultFontsAsync`
- **Duplicazione individuata**: I tre file contengono la medesima struttura di scansione delle embedded resources da assembly, controllo esistenza su `db.FontAssets.AnyAsync`, apertura stream da `asm.GetManifestResourceStream` e registrazione su `fontCatalog.RegisterFontAsync`. L'unica variazione tra le tre classi è l'array statico di tuple `(string Alias, string ResourceFileName, string License)` e la costante `GameKey`.
- **Perché è la stessa responsabilità**: Entrambi caricano font da risorse incorporate verso il catalogo font per un gioco specifico.
- **Proposta di estrazione**: Creare una classe base o un servizio comune `FontSeederBase` (o un unico `GameFontPackageSeeder` guidato da configurazione/record) in `CardMaker.Infrastructure.Storage`:
  ```csharp
  public abstract class GameFontSeederBase(CardMakerDbContext db, IFontCatalog fontCatalog, ILogger logger)
  {
      protected async Task SeedFontsAsync(string gameKey, IReadOnlyList<FontMappingDefinition> mappings, Assembly assembly, CancellationToken ct);
  }
  ```
- **Libreria di destinazione**: `CardMaker.Infrastructure`
- **Impatto**: Riduzione di circa 180 righe duplicate; eliminazione del rischio di disallineamento nei controlli di sicurezza o nei log di caricamento font.
- **Rischio**: Basso (la logica funzionale resta identica, verificata dai test).

### `DUP-002`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File Coinvolti**:
  - `src/CardMaker.Infrastructure/Content/YuGiOhContentSeeder.cs`
  - `src/CardMaker.Infrastructure/Content/PokemonContentSeeder.cs`
  - `src/CardMaker.Infrastructure/Content/MtgContentSeeder.cs`
- **Metodi**: `SeedAsync`
- **Duplicazione individuata**: Oltre 120 righe per file eseguono esattamente lo stesso algoritmo procedurale di navigazione e upsert del grafo:
  1. Ricerca del gioco esistente tramite chiave.
  2. Aggiornamento delle proprietà base del gioco (`Name`, `Description`).
  3. Iterazione su `SymbolSets` e aggiornamento ricorsivo dei `Symbols`.
  4. Iterazione su `OptionLists` e aggiornamento ricorsivo degli `Items`.
  5. Iterazione su `Traits`.
  6. Salvataggio delle modifiche con `db.SaveChangesAsync()`.
- **Perché è la stessa responsabilità**: È il medesimo orchestratore di sincronizzazione del grafo di entità tra definizioni statiche e database relazionale.
- **Proposta di estrazione**: Estrarre un unico orchestratore generico `ContentGraphSeeder` in `CardMaker.Infrastructure.Content` che accetta un'istanza di `SeedGraph` e applica la transazione di aggiornamento in modo centralizzato.
- **Libreria di destinazione**: `CardMaker.Infrastructure`
- **Impatto**: Eliminazione di oltre 300 righe di codice boilerplate soggetto a dimenticanze quando viene aggiunto un nuovo campo al modello.
- **Rischio**: Basso-Medio (richiede test accurato per garantire che l'aggiornamento parziale di chiavi e relazioni non causi conflitti di tracking EF Core).

### `DUP-003`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File Coinvolti**:
  - `src/CardMaker.Infrastructure/Content/YuGiOhSeedData.cs` (linee 15–20)
  - `src/CardMaker.Infrastructure/Content/PokemonSeedData.cs` (linee 15–20)
  - `src/CardMaker.Infrastructure/Content/MtgSeedData.cs` (linee 15–20)
- **Classi/Record**: `public sealed record SeedGraph(...)`
- **Duplicazione individuata**: La definizione del record `SeedGraph` è copiata e incollata integralmente con gli stessi identici membri in tre file separati nello stesso namespace.
- **Perché è la stessa responsabilità**: Rappresenta il contenitore dati in-memory per il seed di un gioco.
- **Proposta di estrazione**: Spostare `SeedGraph` in un file dedicato `src/CardMaker.Infrastructure/Content/SeedGraph.cs` (oppure in `CardMaker.Application.Content`).
- **Libreria di destinazione**: `CardMaker.Infrastructure`
- **Impatto**: Manutenibilità e standardizzazione del contratto di seed.
- **Rischio**: Nullo.

### `DUP-004`
- **Priorità**: P2 (Medium)
- **Categoria**: Problema certo
- **File Coinvolti**:
  - `src/CardMaker.Infrastructure/Storage/PlaceholderSeeder.cs`
- **Metodi**: `SeedYuGiOhAsync`, `SeedPokemonAsync`, `SeedMtgAsync`
- **Duplicazione individuata**: I tre metodi hanno corpi quasi identici: determinano la `CardGeometry` del gioco, instanziano le specifiche di frame e simboli di default, invocano il metodo comune `SeedGameAsync` e ritornano il risultato aggregato.
- **Perché è la stessa responsabilità**: Dispatch del processo di generazione segnaposto parametrizzato dal gioco.
- **Proposta di estrazione**: Parametrizzare il flusso mediante una factory o una tabella di configurazione interna `(GameKey, CardGeometry, Func<PlaceholderSpecRegistry>)`.
- **Libreria di destinazione**: `CardMaker.Infrastructure`
- **Impatto**: Codice più compatto e facilità di estensione a giochi futuri.
- **Rischio**: Nullo.

### `DUP-005`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File Coinvolti**:
  - `src/CardMaker.Infrastructure/Rendering/CardPreviewService.cs` (linee 75–175)
  - `src/CardMaker.Infrastructure/Cards/CardExportService.cs` (linee 167–270)
- **Metodi**: `LoadResourcesAsync`
- **Duplicazione individuata**: Entrambi i metodi caricano le risorse grafiche da DB ed asset store:
  1. Chiamata a `LayoutReferences.Collect(layout, values)`.
  2. Query ad `Assets` per ID e caricamento/decodifica immagini.
  3. Query ad `Assets` per nome originale e fallback segnaposto.
  4. Risoluzione dei simboli (`Symbols` -> `SymbolSet` -> `Asset` -> Sha256).
  5. Risoluzione dei caratteri (`FontAssets` per gioco/alias -> Stream -> SKTypeface).
- **Perché è la stessa responsabilità**: Caricamento preventivo delle dipendenze binarie necessarie a SkiaSharp prima della fase di rendering sincrona.
- **Proposta di estrazione**: Creare l'interfaccia `IRenderResourceLoader` in `CardMaker.Application.Rendering` con implementazione concreta in `CardMaker.Infrastructure.Rendering.RenderResourceLoader`. Sia `CardPreviewService` che `CardExportService` inietteranno questo servizio.
- **Libreria di destinazione**: `CardMaker.Application` (interfaccia) / `CardMaker.Infrastructure` (implementazione).
- **Impatto**: Risolve una duplicazione di 100+ righe di query EF complesse; garantisce che un bugfix nel caricamento degli asset (ad esempio nei percorsi di fallback) si rifletta sia nell'anteprima che nell'export.
- **Rischio**: Basso.

### `DUP-006`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File Coinvolti**:
  - `src/CardMaker.Desktop/Layout/DesktopNavMenu.razor`
  - `src/CardMaker.Web/Components/Layout/NavMenu.razor`
- **Duplicazione individuata**: L'elenco dei link di navigazione utente (`Le mie Carte`, `Nuova Carta`, `Guida Utente`) e dei 9 link del pannello di amministrazione (`Gestione Contenuti`, `Libreria Asset`, `Ruoli Font`, `Segnaposto`, `Prova Motore`, `Inviti Utente`, `Backup & Integrità`, `Registro Audit`, `Guida Admin`) è identico nei testi, nelle icone e nei percorsi href. La sola differenza risiede nell'involucro di autorizzazione (`<AuthorizeView>` con ruoli sul Web vs vista libera senza autenticazione sul Desktop).
- **Perché è la stessa responsabilità**: Albero di navigazione primaria dell'applicazione CardMaker.
- **Proposta di estrazione**: Creare un componente Razor riutilizzabile `SharedNavItems.razor` in `CardMaker.UI/Components/Layout/` che visualizza l'elenco dei link, parametrizzando se renderizzare o meno le voci protette tramite un flag o esponendo un `RenderFragment`.
- **Libreria di destinazione**: `CardMaker.UI`
- **Impatto**: Qualsiasi aggiunta o ridenominazione di una pagina dell'applicazione non richiederà più la modifica sincronizzata su due progetti diversi.
- **Rischio**: Basso.

---

## 5. Refactoring / Spaghetti Code (REF-xxx)

### `REF-001`
- **Problema**: God Class e violazione Single Responsibility Principle (SRP) e Open/Closed Principle (OCP).
- **Posizione**: `src/CardMaker.Rendering/CardRenderer.cs` (1005 righe).
- **Complessità**: Classe monolitica contenente uno switch centrale di 9 rami per il dispatch del disegno e decine di metodi privati:
  - Disegno immagini statiche, slot immagine con fit/fill/crop, simboli singoli e ripetitori a matrice, figure geometriche (rettangoli, ellissi, bordi sfumati, gradienti lineari), blocchi di testo e formattazione Rich Text, toggle group e overlay semitrasparenti.
  - Post-processing per il ritaglio ad angoli arrotondati, applicazione segni di taglio (crop marks), margini di al vivo (bleed) e guide visive di safe zone.
  - Parsing di colori esadecimali, calcolo layout delle matrici e coordinate DPI.
- **Perché è difficile da mantenere**: Qualsiasi modifica alle formule di un layer rischia regressioni su altri layer. L'aggiunta di un nuovo tipo di layer richiede la modifica diretta della classe `CardRenderer` (violazione OCP).
- **Refactoring suggerito**:
  1. Introdurre un'interfaccia interna `ILayerPainter<TLayer> where TLayer : LayerDefinition` o un pattern Strategy:
     ```csharp
     internal interface ILayerPainter
     {
         bool CanPaint(LayerDefinition layer);
         void Paint(SKCanvas canvas, LayerDefinition layer, SKRect dest, double opacity, PaintContext context);
     }
     ```
  2. Suddividere i metodi in classi specializzate nel namespace `CardMaker.Rendering.Painters`:
     - `ImageLayerPainter` (StaticImage, ImageSlot)
     - `SymbolLayerPainter` (SymbolSlot, SymbolRepeater)
     - `ShapeLayerPainter` (ShapeLayer)
     - `TextLayerPainter` (TextLayer, RichTextLayer)
     - `ContainerLayerPainter` (ToggleGroup, GroupLayer)
     - `OverlayLayerPainter` (OverlayLayer)
  3. Spostare la logica di post-processing (angoli arrotondati, crop marks, guide) in una classe `RenderPostProcessor`.
- **Rischio**: Medio (richiede verifica pixel-perfect tramite i test di rendering SkiaSharp esistenti in `CardMaker.Rendering.Tests`).

### `REF-002`
- **Problema**: Componente Razor monolitico ad altissima complessità visiva e procedurale.
- **Posizione**: `src/CardMaker.UI/Pages/Admin/ContentManager.razor` (1280 righe).
- **Complessità**: Il componente raggruppa in un unico file:
  - 5 tab distinte per altrettanti domini: Giochi, Tipi Carta, Tratti/Tag, Set di Simboli, Liste di Opzioni.
  - 5 modal separati per creazione/modifica con i rispettivi form di binding.
  - 16 blocchi `try/catch` duplicati per gestire errori CRUD.
  - Gestione di stato reattivo con dozzine di campi privati (`_selectedGame`, `_editingCardType`, `_isSaving`, `_errorMessage`, etc.).
- **Perché è difficile da mantenere**: Leggere o modificare un flusso CRUD specifico richiede di navigare oltre un migliaio di righe. La manutenibilità e la testabilità dell'interfaccia sono compromesse.
- **Refactoring suggerito**:
  - Decomporre il file principale in una shell di layout a tab e 5 sotto-componenti dedicati situati in `CardMaker.UI/Pages/Admin/ContentTabs/`:
    1. `GamesTab.razor`
    2. `CardTypesTab.razor`
    3. `TraitsTab.razor`
    4. `SymbolSetsTab.razor`
    5. `OptionListsTab.razor`
  - Ogni sotto-componente gestirà i propri eventi e modali, riducendo la dimensione di ciascun file a circa 200 righe.
- **Rischio**: Basso (la UI Blazor supporta naturalmente la composizione dei componenti; zero impatto sul backend).

### `REF-003`
- **Problema**: Violazione del principio architetturale Data-Driven (ADR-001) e Business Logic cablata nella UI.
- **Posizione**: `src/CardMaker.UI/Pages/Cards/CardEditor.razor` (linee 336–435).
- **Complessità**:
  - `UpdateYuGiOhValues`: calcola stringhe complesse come `raceName` ("Drago"), `effectFlag` ("Fusione / Effetto", "Synchro / Effetto", "Link / Effetto"), e costruisce la riga `[Drago / Fusione / Effetto]`.
  - `UpdateMtgValues`: manipola la riga tipo stringa sostituendo programmaticamente prefissi come "Creatura Leggendaria" o "Creatura".
  - `UpdatePokemonValues`: concatena i tratti in formato stringa per il badge dello stadio evolutivo (`stageTraitSuffix`).
- **Perché è difficile da mantenere**: Aggiungere o modificare le regole testuali di un gioco richiede di modificare il sorgente C# dell'editor grafico Blazor. L'applicazione non è puramente data-driven finché i giochi sono discriminati da `if (CurrentGameKey == "yugioh")` nel codice UI.
- **Refactoring suggerito**:
  - Estrarre un servizio di dominio/applicazione per la computazione dei campi derivati: `ICardDerivedValueCalculator` con implementazioni specifiche o, preferibilmente, guidate da espressioni/regole di trasformazione dichiarate nel `GamePackage` / `CardType`.
  - Come primo step intermedio sicuro: spostare i metodi `UpdateYuGiOhValues`, `UpdateMtgValues`, `UpdatePokemonValues` fuori dal file Razor e incapsularli in una classe di servizio nel layer `Application` (`CardDerivedValuesService`), rendendo la UI completamente agnostica rispetto alle chiavi dei giochi.
- **Rischio**: Basso (i calcoli testuali rimangono immutati; il disaccoppiamento migliora drasticamente la testabilità con unit test dedicati).

### `REF-004`
- **Problema**: Accoppiamento tra misurazione caratteri, formattazione testo e disegno su Canvas.
- **Posizione**: `src/CardMaker.Rendering/Text/TextEngine.cs` (333 righe).
- **Complessità**: La classe gestisce font caching, word wrapping, tokenizzazione di simboli inline (`{T}`, `{B}`, `{W}` etc.), calcolo di altezza riga, scaling e disegno su `SKCanvas`.
- **Perché è difficile da mantenere**: Separare la fase di layouting/misura dalla fase di rendering su canvas renderebbe possibile calcolare le dimensioni del testo senza istanziare o interagire con una canvas grafica.
- **Refactoring suggerito**: Separare la logica in due passaggi:
  1. `TextLayoutEngine`: tokenizer, line-breaker e calcolo dei bounding boxes (puramente computazionale).
  2. `TextPainter`: riceve il modello posizionato e disegna glifi e simboli sulla canvas Skia.
- **Rischio**: Basso-Medio (richiede di garantire l'assoluta invarianza tipografica nei test visivi).

---

## 6. Concurrency / Multithreading (CON-xxx)

### `CON-001`
- **Priorità**: P1 (High)
- **Classificazione**: **REQUIRES REVIEW**
- **File**: `src/CardMaker.Infrastructure/Cards/CardExportService.cs`
- **Metodo**: `ExportAsync` (linee 87–119)
- **Operazione**: Generazione fronte e retro per esportazione PDF con `options.BothFaces == true`.
- **Tipo**: CPU-bound (SkiaSharp rendering) + I/O-bound (caricamento risorse da DB/disco).
- **Perché può essere parallelizzata**: Il rendering della facciata anteriore e di quella posteriore sono due operazioni completamente indipendenti. Attualmente il codice esegue prima il fronte e poi il retro in sequenza.
- **Tecnologia suggerita**: `Task.WhenAll`.
- **Thread safety & Rischi**:
  > [!WARNING]
  > **Rischio Concorrenza EF Core**: `LoadResourcesAsync` utilizza `CardMakerDbContext`. In EF Core, un'istanza di `DbContext` **NON è thread-safe**. Se due task chiamassero `LoadResourcesAsync` in parallelo usando la stessa istanza di `db`, si verificherebbe `InvalidOperationException: A second operation was started on this context instance before a previous operation completed`.
- **Soluzione di Concorrenza Sicura**:
  1. Caricare prima le risorse di entrambe le facciate in sequenza (o unire gli identificatori in un'unica query cumulativa).
  2. Eseguire in parallelo tramite `Task.Run` **esclusivamente** le chiamate pure SkiaSharp: `renderer.Render(frontRequest)` e `renderer.Render(backRequest)`. SkiaSharp su due istanze separate di `SKSurface` in-memory è completamente thread-safe.
- **Beneficio atteso**: Dimezzamento del tempo di rendering per export di PDF con fronte e retro a 300/600 DPI.

### `CON-002`
- **Priorità**: P2 (Medium)
- **Classificazione**: Possibile miglioramento
- **File**: `src/CardMaker.Infrastructure/Storage/PlaceholderSeeder.cs`
- **Metodo**: `SeedGameAsync` (linee 49–86 e 97–134)
- **Operazione**: Generazione raster di frame e simboli segnaposto procedurali.
- **Tipo**: CPU-bound (elaborazione grafica SkiaSharp).
- **Perché può essere parallelizzata**: `PlaceholderFrameGenerator.Generate` e `PlaceholderSymbolGenerator.Generate` non accedono al database né allo storage: producono un array di byte PNG a partire da una specifica matematica e geometrica. Attualmente vengono generati uno alla volta in un ciclo foreach sincrono.
- **Tecnologia suggerita**: `Parallel.ForEach` o `Parallel.ForEachAsync` per generare i blob binari in parallelo su tutti i core CPU, raccoglierli in memoria e successivamente salvarli a database in modo sequenziale/transazionale.
- **Rischi**: Minimi se la fase di I/O (upload e persistenza DB) rimane sequenziale.
- **Beneficio atteso**: Riduzione dell'80% del tempo CPU necessario al bootstrap o alla rigenerazione dei segnaposto (da ~3 secondi a meno di 600ms su macchine quad-core).

### `CON-003`
- **Priorità**: P0 (Critical)
- **Classificazione**: **Problema certo / REQUIRES REVIEW**
- **File**: `src/CardMaker.Infrastructure/Rendering/LruCache.cs` e `DecodedImageCache.cs`
- **Operazione**: Eviction e smaltimento (`DisposeIfPossible`) delle immagini SKImage.
- **Tipo**: Memory & Concurrency.
- **Dettaglio del Problema**:
  - `LruCache.Set` esegue `DisposeIfPossible(last.Value.Value)` quando il conteggio supera la capacità (256 elementi).
  - In `CardPreviewService.cs`, un `SKImage` viene ottenuto dalla cache condivisa tramite `GetOrDecodeAsync` e inserito in `PreloadedRenderResources` con `owned: false` (quindi il renderer presuppone che l'immagine rimanga viva per tutta la durata del rendering).
  - Se un altro thread inserisce nuovi asset saturando la cache, l'immagine potrebbe venire evictata e **disposta** mentre il thread di rendering la sta ancora leggendo su `canvas.DrawImage`.
  - Poiché `SKImage` incapsula un puntatore C++ nativo non gestito, l'uso dopo la disallocazione genera un crash di processo (Access Violation / SIGSEGV) o artefatti grafici imprevedibili.
- **Soluzione Proposta**:
  - Modificare la cache affinché restituisca una `SKImage` clonata (`image.ToRasterImage()` o incremento del ref count di Skia) oppure implementare un meccanismo di lease/ref-count: l'immagine non viene smaltita finché è in prestito a un'operazione di render attiva.
- **Beneficio atteso**: Prevenzione assoluta di race conditions e crash improvvisi dell'host durante render concorrenti intensi.

---

## 7. Async/Await (ASYNC-xxx)

### `ASYNC-001`
- **Priorità**: P2 (Medium)
- **Categoria**: Problema certo
- **File**: `src/CardMaker.Infrastructure/Admin/BackupService.cs` (linee 63–77)
- **Metodo**: `ListBackupsAsync`
- **Problema**: Il metodo restituisce `Task.FromResult<IReadOnlyList<BackupFileInfo>>(files)` ma esegue internamente `Directory.GetFiles(...)` che è un'operazione di I/O sincrona bloccante sul thread corrente.
- **Proposta**: Utilizzare `Directory.EnumerateFiles` in combinazione con un flusso asincrono o delegare la lettura a I/O asincrono non bloccante.
- **Rischio**: Nullo.

### `ASYNC-002`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File**: `src/CardMaker.Infrastructure/Cards/CardExportService.cs` (linee 62, 94, 111, 127)
- **Metodo**: `ExportAsync`
- **Problema**: Uso promiscuo di `Task.Run(async () => ...)` all'interno di metodi di servizio. Nel contesto Web ASP.NET Core, invocare `Task.Run` per offloadare lavoro sottrae un thread al ThreadPool senza risparmiare thread worker. Nel contesto Desktop (Blazor Hybrid con Photino), invece, `Task.Run` è indispensabile per non congelare l'interfaccia utente.
- **Proposta**:
  - Mantenere i metodi del servizio puramente computazionali e asincroni diretti (`async Task<CardExportResult>`), lasciando al chiamante della UI (es. Blazor Desktop) la decisione di eseguire il lavoro su un background worker tramite `Task.Run` prima di aggiornare lo stato del componente.
- **Rischio**: Basso (richiede verifica manuale dei flussi di export nell'app Desktop).

### `ASYNC-003`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File**: `src/CardMaker.Application/Assets/IAssetCatalog.cs` e `src/CardMaker.Infrastructure/Storage/AssetService.cs`
- **Problema**: Perdita del passaggio di `CancellationToken` in alcuni overload di navigazione e caricamento stream.
- **Proposta**: Rendere obbligatorio o con default `= default` il parametro `CancellationToken cancellationToken` in tutti i metodi asincroni di `IAssetCatalog` e propagarlo a `stream.CopyToAsync` ed alle query EF Core.
- **Rischio**: Nullo.

---

## 8. Database & EF Core (DB-xxx)

### `DB-001`
- **Priorità**: P0 (Critical)
- **Categoria**: **Problema certo**
- **File**: `src/CardMaker.Infrastructure/Storage/PlaceholderSeeder.cs` (linee 54 e 103)
- **Metodo**: `SeedGameAsync`
- **Problema**: **Collo di bottiglia N+1 query devastante**. All'interno di ciascuna iterazione del ciclo di generazione dei frame (30+ iterazioni) e di ciascun simbolo (60+ iterazioni) viene eseguita la riga:
  ```csharp
  var before = await catalog.ListAsync(targetGameId, 500, cancellationToken).ConfigureAwait(false);
  ```
  seguita da:
  ```csharp
  if (before.Any(a => a.Id == outcome.Asset!.Id))
  ```
  Vengono eseguite circa 90 query a database, ciascuna delle quali richiede la scansione e la materializzazione di centinaia di record, esclusivamente per verificare se l'asset appena registrato esisteva già.
- **Proposta**:
  1. Caricare l'elenco degli asset o i loro ID/OriginalFileName **una sola volta** prima dell'ingresso nei cicli in un `HashSet<string>` o `HashSet<Guid>`.
  2. In alternativa, far restituire direttamente al metodo `UploadAsync` un flag `bool IsNewAsset` nell'oggetto risultato (`AssetUploadResult`).
- **Beneficio atteso**: Il tempo di seeding dei placeholder passa da oltre 4-5 secondi a circa 200 millisecondi.
- **Rischio**: Nullo.

### `DB-002`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File**: `src/CardMaker.Infrastructure/Admin/BackupService.cs` (linee 39 e 94)
- **Problema**: Accoppiamento rigido e diretto alla sintassi SQL proprietaria di SQLite:
  - Linea 39: `await db.Database.ExecuteSqlAsync($"VACUUM INTO {backupPath};", cancellationToken);`
  - Linea 94: `cmd.CommandText = "PRAGMA integrity_check;";`
  Se la solution dovesse essere configurata per operare su PostgreSQL, SQL Server o un altro database relazionale, questo servizio genererebbe eccezioni irreversibili a runtime.
- **Proposta**:
  - Introdurre un'astrazione `IDatabaseSnapshotProvider` con implementazione concreta `SqliteDatabaseSnapshotProvider`. Nel caso in cui il provider corrente non sia SQLite, il provider restituirà un report di operazione non supportata o implementerà la strategia di backup specifica del database.
- **Rischio**: Basso.

### `DB-003`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File**: Intera solution (`CardMaker.Infrastructure` e `CardMaker.Application`)
- **Problema**: Mancanza di un'astrazione Repository coerente; uso promiscuo di `CardMakerDbContext` iniettato direttamente nei servizi applicativi (`CardService`, `AssetService`, `BackupService`, `ContentSeeders`).
- **Proposta**:
  - Non è necessario introdurre il pattern Repository completo per ogni singola entità (EF Core `DbSet` funge già da repository). Tuttavia, è opportuno incapsulare le query complesse riutilizzate (es. `GetCardWithFullDetailsAsync`, `GetAssetsByGameAsync`) in extension methods su `IQueryable<T>` o tramite specifiche di query per evitare frammentazione della logica di filtraggio.
- **Rischio**: Basso.

---

## 9. Filesystem & Storage (FS-xxx)

### `FS-001`
- **Priorità**: P1 (High)
- **Categoria**: Problema certo
- **File**:
  - `src/CardMaker.Infrastructure/Admin/BackupService.cs` (linee 22, 33, 35, 66)
  - `src/CardMaker.Desktop/Program.cs` (linee 65–75)
- **Problema**: Chiamate dirette alle API statiche di `System.IO.File` e `System.IO.Directory`.
- **Dettaglio**: `BackupService` crea directory, controlla l'esistenza di file e calcola percorsi fisici hardcoded. Ciò impedisce:
  - Il testing unitario isolato senza effetti collaterali sul disco locale.
  - L'eventuale supporto futuro per l'archiviazione di backup su percorsi di rete (NAS) o Cloud Storage (AWS S3, Azure Blob, Google Cloud Storage).
- **Proposta**: Introdurre un'interfaccia mirata `IBackupStorageProvider` o estendere `IAssetStore` con primitive di gestione file astratte (`CreateDirectoryAsync`, `FileExistsAsync`, `DeleteFileAsync`, `ListFilesAsync`).
- **Rischio**: Nullo.

### `FS-002`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File**: `src/CardMaker.Infrastructure/Storage/FileSystemAssetStore.cs`
- **Problema**: Gestione dei percorsi file tramite concatenazione e hashing su due livelli di cartelle senza lock a livello di filesystem durante la scrittura concorrente di asset con lo stesso SHA-256.
- **Proposta**: Verificare l'uso di file temporanei con ridenominazione atomica (`File.Move` atomico su filesystem POSIX e NTFS) per prevenire file troncati in caso di crash o interruzione di corrente durante l'upload.
- **Rischio**: Basso.

---

## 10. Desktop vs Web UI (UI-xxx)

### `UI-001`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File Coinvolti**:
  - `src/CardMaker.Desktop/Layout/DesktopNavMenu.razor`
  - `src/CardMaker.Web/Components/Layout/NavMenu.razor`
- **Problema**: Duplicazione strutturale del menu di navigazione (già catalogata in `DUP-006`). La presenza di due file distinti impone di mantenere manualmente allineate le rotte e i badge delle sezioni.
- **Proposta**: Spostare il menu di navigazione principale in `CardMaker.UI/Components/Layout/AppNavMenu.razor`, configurando la visibilità dei ruoli tramite un `CascadingParameter` o iniettando un'interfaccia di autorizzazione UI astratta (`IAppSecurityPolicy`).
- **Rischio**: Nullo.

### `UI-002`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File Coinvolti**:
  - `src/CardMaker.Desktop/Layout/DesktopMainLayout.razor`
  - `src/CardMaker.Web/Components/Layout/MainLayout.razor`
- **Problema**: Entrambi i file duplicano la logica di transizione delle pagine:
  - L'iscrizione a `NavigationManager.LocationChanged`.
  - La gestione della classe CSS animata `cm-page-animating`.
  - L'overlay di caricamento globale `<GlobalLoadingOverlay />`.
  - La struttura della topbar con il pulsante toggle della sidebar e il pulsante per il tema chiaro/scuro.
- **Proposta**: Creare in `CardMaker.UI` un layout base riutilizzabile `BaseMainLayout.razor` da cui `DesktopMainLayout` e `WebMainLayout` ereditano o che incapsulano, variando solo le aree specifiche (come il box di login utente).
- **Rischio**: Basso.

---

## 11. Performance (PERF-xxx)

### `PERF-001`
- **Priorità**: P0 (Critical)
- **Categoria**: **Problema certo**
- **File**: `src/CardMaker.Infrastructure/Storage/PlaceholderSeeder.cs`
- **Problema**: Query ripetute a database all'interno del loop di generazione (Dettagliata in `DB-001`). È il singolo fattore di rallentamento principale durante l'inizializzazione del software.

### `PERF-002`
- **Priorità**: P1 (High)
- **Categoria**: Possibile miglioramento
- **File**: `src/CardMaker.Rendering/CardRenderer.cs` (linee 83–110)
- **Metodo**: `CollectVisibleLayers`
- **Problema**: Allocazioni multiple di collezioni intermedie e ordinamento LINQ a ogni singolo frame di rendering:
  ```csharp
  return [.. result.OrderBy(item => item.Item1.Z)];
  ```
  Inoltre, l'albero dei layer viene ricorsivamente esplorato e appiattito a ogni invocazione di `Render`, anche durante il live-typing nell'editor quando cambia solo un valore testuale ma la struttura dei layer è identica.
- **Proposta**:
  - Evitare `.OrderBy()` con allocazioni LINQ; utilizzare `result.Sort((a, b) => a.Layer.Z.CompareTo(b.Layer.Z))` sul posto.
  - Valutare la memoizzazione/caching dell'albero dei layer visibili per un dato template finché non cambiano le variabili che influenzano `VisibleWhen`.
- **Beneficio atteso**: Riduzione delle allocazioni GC gen0 del 30% durante l'anteprima in tempo reale.
- **Rischio**: Basso.

### `PERF-003`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File**: `src/CardMaker.Rendering/Text/RichText.cs`
- **Problema**: Parsing con espressioni regolari eseguito a ogni render di layer RichText. Se il testo contiene decine di simboli inline (`{T}`, `{B}`, etc.), le regex allocate producono carichi non trascurabili.
- **Proposta**: Utilizzare i sorgenti generati di .NET con `[GeneratedRegex]` o uno scanner a token lineare a passaggio singolo (`ReadOnlySpan<char>`).
- **Beneficio atteso**: Parsing 4x più rapido con zero allocazioni su stringhe intermedie.
- **Rischio**: Nullo.

---

## 12. Memory & Resource Management (MEM-xxx)

### `MEM-001`
- **Priorità**: P0 (Critical)
- **Categoria**: **Problema certo / REQUIRES REVIEW**
- **File**: `src/CardMaker.Infrastructure/Rendering/LruCache.cs`
- **Problema**: Mancanza di reference counting sugli oggetti `SKImage` smaltiti dalla cache LRU (descritta in `CON-003`).

### `MEM-002`
- **Priorità**: P1 (High)
- **Categoria**: Possibile miglioramento
- **File**: `src/CardMaker.Rendering/CardRenderer.cs` (linee 35–65)
- **Problema**: Allocazione di nuove superfici SkiaSharp (`SKSurface.Create`) per ogni singola richiesta di render, incluse le anteprime a bassa risoluzione (72 DPI) generate continuamente durante la digitazione nell'editor.
- **Proposta**:
  - Valutare un `ArrayPool<byte>` o un meccanismo di riuso per i buffer grafici intermedi di rendering dove applicabile, oppure garantire che le istanze di `SKSurface` e `SKBitmap` vengano rigorosamente racchiuse in blocchi `using` (attualmente la maggior parte ha `using`, ma il carico sul Garbage Collector per i puntatori nativi di SkiaSharp resta elevato).
- **Rischio**: Basso.

### `MEM-003`
- **Priorità**: P2 (Medium)
- **Categoria**: Problema certo
- **File**: `src/CardMaker.UI/Pages/Cards/CardEditor.razor` e altri componenti Blazor
- **Problema**: `CancellationTokenSource` multipli generati per il debounce dell'anteprima (`_debounceCts`). Se non smaltiti prima di assegnarne uno nuovo, possono verificarsi leak minori di handle.
- **Proposta**: Chiamare sempre `_debounceCts?.Cancel(); _debounceCts?.Dispose();` prima di istanziare un nuovo CTS.
- **Rischio**: Nullo.

---

## 13. Error Handling (ERR-xxx)

### `ERR-001`
- **Priorità**: P2 (Medium)
- **Categoria**: Problema certo
- **File**: `src/CardMaker.UI/Pages/Admin/ContentManager.razor` (16 occorrenze)
- **Problema**: Duplicazione massiva di blocchi `catch (Exception ex)` che eseguono esclusivamente l'assegnazione:
  ```csharp
  catch (Exception ex)
  {
      _errorMessage = ex.Message;
  }
  ```
  senza logging strutturato, senza correlazione dell'errore e senza distinzione tra violazioni di vincoli di unicità del database (es. chiave duplicata) ed errori imprevisti di I/O.
- **Proposta**: Introdurre un handler comune per le operazioni UI asincrone o un wrapper `ExecuteSafeAsync(Func<Task> action, string userMessage)` che intercetta le eccezioni, le traccia sul logger e popola il messaggio per l'utente in modo uniforme.
- **Rischio**: Nullo.

### `ERR-002`
- **Priorità**: P2 (Medium)
- **Categoria**: Possibile miglioramento
- **File**: `src/CardMaker.Application/Content/TemplateSelector.cs` (linee 47–50)
- **Problema**: Eccezione silenziosa ignorata:
  ```csharp
  catch (JsonException)
  {
      continue;
  }
  ```
  Se il JSON della regola di selezione del template (`SelectionRuleJson`) è malformato, l'errore viene completamente silenziato senza emettere alcun log o warning diagnostico. L'utente o l'amministratore non hanno modo di capire perché un template non viene mai selezionato.
- **Proposta**: Iniettare `ILogger<TemplateSelector>` e registrare un `LogWarning` quando viene riscontrata una regola JSON sintatticamente non valida.
- **Rischio**: Nullo.

---

## 14. Abstractions & Dependency Inversion

Dall'analisi sistematica delle dipendenze concrete emergono tre aree in cui l'introduzione mirata di astrazioni produce un reale beneficio architetturale senza aggiungere complessità fine a se stessa:

1. **`IExpressionEvaluator` e `IValueBinder`**  
   - *Collocazione*: `CardMaker.Contracts` (o `CardMaker.Domain.Expressions`).  
   - *Motivazione*: Permette a `TemplateSelector` (in `CardMaker.Application`) di valutare le condizioni senza referenziare il motore grafico `CardMaker.Rendering`, ripristinando la conformità a Clean Architecture.

2. **`IRenderResourceLoader`**  
   - *Collocazione*: `CardMaker.Application.Rendering` (interfaccia) / `CardMaker.Infrastructure.Rendering` (implementazione).  
   - *Motivazione*: Centralizza il caricamento, il decoding, il caching e la risoluzione dei fallback per font, simboli e immagini, eliminando la duplicazione identica tra `CardPreviewService` e `CardExportService`.

3. **`IDatabaseSnapshotProvider`**  
   - *Collocazione*: `CardMaker.Application.Admin` (interfaccia) / `CardMaker.Infrastructure.Admin` (implementazione).  
   - *Motivazione*: Disaccoppia la business logic di backup dai comandi raw specifici di SQLite (`VACUUM INTO`), consentendo in futuro l'adozione trasparente di altri database engine.

---

## 15. Proposed Target Architecture

L'architettura futura target rappresenta un'evoluzione pulita e graduale della soluzione attuale:

```
┌─────────────────────────────────────────────────────────────┐
│                 Presentation Layer (Frontend)               │
│                                                             │
│   ┌─────────────────────────┐     ┌─────────────────────┐   │
│   │    CardMaker.Desktop    │     │    CardMaker.Web    │   │
│   │   (Photino Blazor App)  │     │  (ASP.NET Core App) │   │
│   └────────────┬────────────┘     └──────────┬──────────┘   │
│                │                             │              │
│                └──────────────┬──────────────┘              │
│                               ▼                             │
│               ┌───────────────────────────────┐             │
│               │         CardMaker.UI          │             │
│               │ (Shared Blazor Razor Library) │             │
│               └───────────────┬───────────────┘             │
└───────────────────────────────┼─────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│                                                             │
│               ┌───────────────────────────────┐             │
│               │     CardMaker.Application     │             │
│               │ (Use Cases, DTOs, Services)   │             │
│               │  * Non referenzia Rendering * │             │
│               └───────────────┬───────────────┘             │
└───────────────────────────────┼─────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        ▼                                               ▼
┌───────────────────────────────┐       ┌─────────────────────────────┐
│        Contracts Layer        │       │        Domain Layer         │
│                               │       │                             │
│     CardMaker.Contracts       │◄──────┤      CardMaker.Domain       │
│  (Layout, Geometry, AST,      │       │ (Entities, Value Objects,   │
│   Expression Evaluator)       │       │  Domain Rules)              │
└───────────────▲───────────────┘       └──────────────▲──────────────┘
                │                                      │
                ├──────────────────────────────────────┘
                │
┌───────────────┴─────────────────────────────────────────────┐
│                   Infrastructure Layer                      │
│                                                             │
│   ┌───────────────────────────┐ ┌─────────────────────────┐ │
│   │    CardMaker.Rendering    │ │CardMaker.Infrastructure │ │
│   │  (SkiaSharp, HarfBuzz,    │ │ (EF Core, Sqlite, Disk, │ │
│   │   Layer Painters, PDF)    │ │  Seeders, ResourceLoader│ │
│   └───────────────────────────┘ └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Principali Cambiamenti nell'Architettura Target
1. `CardMaker.Application` non referenzia più `CardMaker.Rendering`.
2. I componenti di calcolo logico (`ConditionEvaluator`, `ValueBinder`) risiedono in `CardMaker.Contracts` o in un sottomodulo puro.
3. `CardMaker.Rendering` adotta un'architettura a Strategy Pattern (`ILayerPainter`) per i singoli tipi di layer.
4. `CardMaker.Infrastructure` accentra il caricamento risorse tramite `RenderResourceLoader`.

---

## 16. Prioritized Implementation Plan

L'implementazione deve avvenire in step ordinati e sequenziali, rispettando le dipendenze tra i layer:

### STEP 1: Correzione Architetturale Fondamentale (Clean Architecture)
- **Obiettivo**: Disaccoppiare `Application` da `Rendering`.
- **Attività**:
  1. Spostare `ValueBinder.cs` e `ConditionEvaluator.cs` da `CardMaker.Rendering.Pipeline` a `CardMaker.Contracts.Layout` (o modulo espressioni).
  2. Rimuovere `<ProjectReference Include="..\CardMaker.Rendering\CardMaker.Rendering.csproj" />` da `CardMaker.Application.csproj`.
  3. Verificare e compilare: tutti i test esistenti devono compilare e passare.
- **ID Correlati**: `ARCH-001`.

### STEP 2: Eliminazione del Collo di Bottiglia Critico (Performance & Database)
- **Obiettivo**: Abbattere il tempo di seeding dei placeholder.
- **Attività**:
  1. In `PlaceholderSeeder.cs`, eliminare la chiamata a `catalog.ListAsync(500)` interna ai cicli foreach.
  2. Implementare il pre-caricamento dell'indice o il flag `IsNew` in `UploadAsync`.
  3. Eseguire in parallelo la generazione dei byte PNG dei frame e dei simboli tramite `Parallel.ForEach`.
- **ID Correlati**: `DB-001`, `PERF-001`, `CON-002`.

### STEP 3: Consolidamento e De-duplicazione dei Seeder (DUP)
- **Obiettivo**: Eliminare oltre 500 righe di codice duplicato tra seeder di font e contenuti.
- **Attività**:
  1. Estrarre il record unico `SeedGraph` in `CardMaker.Infrastructure.Content` (`DUP-003`).
  2. Creare `GameFontSeederBase` ed ereditare in `YuGiOhFontSeeder`, `PokemonFontSeeder`, `MtgFontSeeder` (`DUP-001`).
  3. Creare `ContentGraphSeeder` per unificare l'algoritmo di upsert in `YuGiOhContentSeeder`, `PokemonContentSeeder`, `MtgContentSeeder` (`DUP-002`).
  4. Semplificare `PlaceholderSeeder` (`DUP-004`).

### STEP 4: Estrazione di `RenderResourceLoader` e Sicurezza Concorrenza (DUP & MEM)
- **Obiettivo**: Unificare il caricamento risorse tra anteprima ed export e risolvere la race condition in cache.
- **Attività**:
  1. Creare `IRenderResourceLoader` e implementare `RenderResourceLoader` (`DUP-005`).
  2. Rifattorizzare `CardPreviewService` e `CardExportService` per iniettare il loader comune.
  3. Mettere in sicurezza `LruCache` e `DecodedImageCache` aggiungendo clonazione o lease check prima di effettuare il dispose (`CON-003`, `MEM-001`).
  4. Parallelizzare in sicurezza il rendering SkiaSharp fronte/retro nell'export PDF (`CON-001`).

### STEP 5: Decomposizione dei Monoliti (Spaghetti Code & UI)
- **Obiettivo**: Aumentare drasticamente la manutenibilità dei due componenti più grandi della soluzione.
- **Attività**:
  1. Rifattorizzare `CardRenderer.cs` estraendo i singoli `ILayerPainter` (`REF-001`).
  2. Suddividere `ContentManager.razor` nei 5 sotto-componenti tab (`REF-002`).
  3. Disaccoppiare la formattazione specifica dei giochi da `CardEditor.razor` in un servizio di dominio/applicazione (`REF-003`).
  4. Unificare la navigazione comune Desktop/Web (`DUP-006`, `UI-001`, `UI-002`).

### STEP 6: Rifinitura, Error Handling e Astrazioni Database/Storage
- **Obiettivo**: Resilienza ed estendibilità futura.
- **Attività**:
  1. Standardizzare l'error handling nei componenti Blazor (`ERR-001`).
  2. Aggiungere logging diagnostico su regole non valide in `TemplateSelector` (`ERR-002`).
  3. Introdurre `IDatabaseSnapshotProvider` e disaccoppiare `BackupService` da comandi raw SQLite (`DB-002`, `FS-001`).
  4. Migliorare le performance di parsing regex con `[GeneratedRegex]` (`PERF-003`).

---

## 17. Files Impacted Matrix

| File | Percorso | Motivo della Modifica | ID Correlati | Priorità |
| :--- | :--- | :--- | :--- | :--- |
| `CardMaker.Application.csproj` | `src/CardMaker.Application/` | Rimozione dipendenza da `CardMaker.Rendering` | `ARCH-001` | P1 |
| `TemplateSelector.cs` | `src/CardMaker.Application/Content/` | Spostamento namespace dipendenze binder / condition | `ARCH-001`, `ERR-002` | P1 |
| `ConditionEvaluator.cs` | `src/CardMaker.Rendering/Pipeline/` -> `CardMaker.Contracts/` | Spostamento nel layer contratti | `ARCH-001` | P1 |
| `ValueBinder.cs` | `src/CardMaker.Rendering/Pipeline/` -> `CardMaker.Contracts/` | Spostamento nel layer contratti | `ARCH-001` | P1 |
| `PlaceholderSeeder.cs` | `src/CardMaker.Infrastructure/Storage/` | Risoluzione N+1 query loop e parallelizzazione Skia | `DB-001`, `PERF-001`, `CON-002`, `DUP-004` | P0 |
| `LruCache.cs` | `src/CardMaker.Infrastructure/Rendering/` | Fix race condition e dispose di SKImage in uso | `CON-003`, `MEM-001` | P0 |
| `CardExportService.cs` | `src/CardMaker.Infrastructure/Cards/` | De-duplicazione loader risorse e parallelismo sicuro | `DUP-005`, `CON-001`, `ASYNC-002` | P1 |
| `CardPreviewService.cs` | `src/CardMaker.Infrastructure/Rendering/` | De-duplicazione loader risorse | `DUP-005` | P1 |
| `YuGiOhFontSeeder.cs` | `src/CardMaker.Infrastructure/Storage/` | Estrazione base class comune | `DUP-001` | P1 |
| `PokemonFontSeeder.cs` | `src/CardMaker.Infrastructure/Storage/` | Estrazione base class comune | `DUP-001` | P1 |
| `MtgFontSeeder.cs` | `src/CardMaker.Infrastructure/Storage/` | Estrazione base class comune | `DUP-001` | P1 |
| `YuGiOhContentSeeder.cs` | `src/CardMaker.Infrastructure/Content/` | Estrazione algoritmo di sync grafo comune | `DUP-002`, `DUP-003` | P1 |
| `PokemonContentSeeder.cs` | `src/CardMaker.Infrastructure/Content/` | Estrazione algoritmo di sync grafo comune | `DUP-002`, `DUP-003` | P1 |
| `MtgContentSeeder.cs` | `src/CardMaker.Infrastructure/Content/` | Estrazione algoritmo di sync grafo comune | `DUP-002`, `DUP-003` | P1 |
| `CardRenderer.cs` | `src/CardMaker.Rendering/` | Decomposizione in Layer Painters (Strategy Pattern) | `REF-001`, `PERF-002`, `MEM-002` | P1 |
| `ContentManager.razor` | `src/CardMaker.UI/Pages/Admin/` | Decomposizione tab e standardizzazione catch | `REF-002`, `ERR-001` | P2 |
| `CardEditor.razor` | `src/CardMaker.UI/Pages/Cards/` | Rimozione logica di gioco hardcoded | `REF-003`, `MEM-003` | P1 |
| `BackupService.cs` | `src/CardMaker.Infrastructure/Admin/` | Astrazione provider snapshot e filesystem I/O | `DB-002`, `FS-001`, `ASYNC-001` | P1 |
| `DesktopNavMenu.razor` | `src/CardMaker.Desktop/Layout/` | Unificazione componenti di navigazione | `DUP-006`, `UI-001` | P2 |
| `NavMenu.razor` | `src/CardMaker.Web/Components/Layout/` | Unificazione componenti di navigazione | `DUP-006`, `UI-001` | P2 |

---

## 18. Risk Matrix

| ID Modifica | Beneficio Principale | Rischio Principale | Priorità | Richiede Review? |
| :--- | :--- | :--- | :--- | :--- |
| **`ARCH-001`** | Rispetta Clean Architecture, disaccoppia Application da Skia | Modifica ai namespace di importazione | **P1** | No |
| **`DB-001`** | Taglia del 95% i tempi di inizializzazione e seed dei placeholder | Nessuno se gli asset sono pre-indicizzati | **P0** | No |
| **`CON-001`** | Dimezza i tempi di export PDF a due facciate | Conflitto multithreading se si condivide `DbContext` | **P1** | **REQUIRES REVIEW** |
| **`CON-002`** | Generazione segnaposto multi-core ultra rapida | Picco di utilizzo CPU durante il bootstrap | **P2** | No |
| **`CON-003`** | Previene crash nativi Skia (access violation/use-after-free) | Possibile aumento della memoria se non si rilascia | **P0** | **REQUIRES REVIEW** |
| **`DUP-001`** | Manutenzione centralizzata font mapping | Nessuno | **P1** | No |
| **`DUP-002`** | Manutenzione centralizzata aggiornamento grafi seed | Tracking EF Core sulle entità collegate | **P1** | No |
| **`DUP-005`** | Singolo punto di caricamento risorse grafiche | Nessuno | **P1** | No |
| **`REF-001`** | Estendibilità a nuovi layer senza modificare il Renderer | Minime discrepanze visive se non testato | **P1** | **REQUIRES REVIEW** |
| **`REF-002`** | `ContentManager` leggibile, modulare e manutenibile | Regressione su eventi di binding Blazor | **P2** | No |
| **`REF-003`** | Ritorno a un'architettura 100% data-driven (ADR-001) | Riformattazione inattesa delle stringhe utente | **P1** | No |
| **`DB-002`** | Portabilità su PostgreSQL / SQL Server | Incompatibilità con tool di backup terzi | **P1** | No |
| **`FS-001`** | Testabilità unitaria del backup service | Nessuno | **P2** | No |

---

## 19. Items Requiring Review (REQUIRES REVIEW)

Gli elementi seguenti comportano considerazioni tecniche critiche o potenziali trade-off architetturali e richiedono approvazione esplicita prima della loro implementazione:

### 1. `CON-001`: Parallelizzazione Esportazione Fronte e Retro (`CardExportService`)
- **Domanda di Design**: Quando l'utente richiede l'esportazione di entrambe le facciate in PDF a 600 DPI, vogliamo pre-caricare tutte le risorse prima e parallelizzare solo il calcolo SkiaSharp, oppure iniettare `IDbContextFactory<CardMakerDbContext>` per rendere le due pipeline di caricamento completamente asincrone e indipendenti?
- **Raccomandazione**: Pre-caricare gli asset con una query cumulativa ed eseguire in parallelo solo `renderer.Render(...)` via `Task.Run` su due thread CPU distinti. Questo evita il sovraccarico di connessioni multiple a SQLite, che opera in modalità file singolo.

### 2. `CON-003` / `MEM-001`: Ciclo di Vita delle Immagini in `LruCache` e Gestione SKImage
- **Domanda di Design**: È preferibile clonare l'immagine al momento del recupero (`image.ToRasterImage()`), oppure trasformare la cache in un meccanismo con reference counting / lease esplicito (`using var lease = cache.Acquire(key)`)?
- **Raccomandazione**: L'approccio con `Lease` (reference count atomico con `Interlocked.Increment/Decrement`) è il più efficiente perché evita duplicazioni di byte in memoria per texture grafiche pesanti (es. sfondi 4K), garantendo al tempo stesso che `Dispose()` venga invocato solo quando sia la cache che tutti i render attivi hanno terminato l'uso.

### 3. `REF-001`: Decomposizione di `CardRenderer` in Strategy Painters
- **Domanda di Design**: La suddivisione in `ILayerPainter` espone l'oggetto interno `SKCanvas` a classi distinte. Vogliamo mantenere i painter `internal` al progetto `CardMaker.Rendering` per evitare di esporre dettagli di implementazione ai consumatori della libreria?
- **Raccomandazione**: Sì, mantenere l'interfaccia `ILayerPainter` e le classi concrete rigorosamente con visibilità `internal`, lasciando l'API pubblica di `ICardRenderer` e `CardRenderer` perfettamente identica a quella attuale.

---

## 20. Summary

L'analisi condotta conferma che la solution **CardMaker** poggia su fondamenta solide, con un'ottima separazione dei layout e una copertura di test che costituisce una rete di sicurezza ideale.

Tuttavia, con la crescita della codebase sono emersi:
1. Un collo di bottiglia critico nel bootstrap dei placeholder (`DB-001`), risolvibile con facilità immediata.
2. Una violazione di Clean Architecture tra `Application` e `Rendering` (`ARCH-001`), che appesantisce il layer dei casi d'uso con dipendenze native.
3. Duplicazioni rilevanti nei seeder di contenuti e font (`DUP-001`, `DUP-002`).
4. Due classi ad altissima complessità (`CardRenderer` e `ContentManager`) che richiedono una scomposizione mirata in moduli specializzati.
5. Una deviazione dal principio data-driven in `CardEditor.razor` (`REF-003`), dove regole di gioco sono state indebitamente introdotte nel codice Blazor.

Tutti gli interventi proposti mantengono **rigorosamente invariata la logica funzionale, i comportamenti utente e i risultati visivi**, migliorando in modo sostanziale la modularità, la velocità di esecuzione e la robustezza concorrente dell'intera piattaforma.
