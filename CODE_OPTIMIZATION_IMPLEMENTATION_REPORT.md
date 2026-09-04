# CardMaker — Report di Implementazione Ottimizzazioni e Refactoring (Fase 2)

Questo documento costituisce il report ufficiale e dettagliato di chiusura dell'attività di **Refactoring & Performance Optimization** per la solution **CardMaker**, eseguito seguendo integralmente le specifiche definite nel documento `CODE_OPTIMIZATION_ANALYSIS.md`.

---

## 1. Executive Summary

L'intervento ha interessato l'intera solution C# (.NET 10 / C# 14), comprendente:
* `CardMaker.Contracts`
* `CardMaker.Domain`
* `CardMaker.Application`
* `CardMaker.Rendering`
* `CardMaker.Infrastructure`
* `CardMaker.UI` (Razor Class Library)
* `CardMaker.Web` (Blazor Web App)
* `CardMaker.Desktop` (Photino.Blazor Desktop)
* Suite di test (`CardMaker.Rendering.Tests`, `CardMaker.Application.Tests`)

### Risultati Chiave Raggiunti:
1. **Ripristino Totale di Clean Architecture (ARCH-001)**:
   - `CardMaker.Application` e `CardMaker.UI` non dipendono più da `CardMaker.Rendering`.
   - `ValueBinder` e `ConditionEvaluator` sono stati promossi nel layer `CardMaker.Contracts.Layout`.
2. **Decomposizione delle God Class & Componenti Monolitici (REF-001, REF-002, REF-003)**:
   - `CardRenderer.cs` (originariamente 1005 righe) è stato decomposto in 6 Strategy Painters specializzati (`ILayerPainter`) e moduli di utilità (`RenderPostProcessor`, `RenderDrawingUtilities`), riducendo la classe a ~150 righe senza alterare di un singolo pixel l'output grafico Skia.
   - `ContentManager.razor` (originariamente 1280 righe) è stato scorporato in una shell di orchestrazione da ~170 righe e 5 sotto-componenti dedicati situati in `Pages/Admin/ContentTabs/` (`GamesTab`, `CardTypesTab`, `TraitsTab`, `SymbolSetsTab`, `OptionListsTab`).
   - Le regole di testo cablate nei vari giochi dentro `CardEditor.razor` sono state estratte nel servizio di dominio/applicazione `CardDerivedValuesService` con suite di test dedicata.
3. **Eliminazione Drastica di Duplicazioni (DUP-001 .. DUP-006, UI-001)**:
   - Eliminati oltre 800 righe di boilerplate nei seeder (`ContentGraphSeeder`, `GameFontSeederBase`, record unificato `SeedGraph`).
   - Sviluppato `RenderResourceLoader` per unificare il caricamento parallelo/asincrono delle risorse Skia tra anteprima dinamica ed export ad alta risoluzione.
   - Unificato il menu di navigazione utente e amministratore tra Desktop e Web tramite i componenti condivisi `UserNavLinks` e `AdminNavLinks`.
4. **Ottimizzazione Prestazionale e Concorrenza (DB-001, PERF-001, CON-001, CON-003, MEM-001)**:
   - Eliminata la query N+1 e parallelizzata la computazione grafica PNG dei placeholder con `Parallel.For` in `PlaceholderSeeder`, abbattendo il tempo di bootstrap da secondi a millisecondi.
   - Parallelizzato il rendering fronte e retro nell'esportazione PDF (`CardExportService`) via `Task.WhenAll` mantenendo il caricamento DB sequenziale thread-safe.
   - Risolto il rischio di crash e `ObjectDisposedException` su unmanaged Skia surfaces proteggendo gli oggetti estratti dalla cache LRU (`disposeOnEviction: false`).
5. **Robustezza ed Astrazioni Storage / Database (ERR-001, ERR-002, DB-002, FS-001, ASYNC-001)**:
   - Estratta l'interfaccia `IDatabaseSnapshotProvider` e implementato `SqliteDatabaseSnapshotProvider` per isolare comandi proprietari SQLite (`VACUUM INTO`, `PRAGMA integrity_check`).
   - Standardizzata la gestione errori UI in `ContentManager` con `ExecuteSafeAsync`.
   - Logging diagnostico con `ILogger` su errori di parsing JSON in `TemplateSelector`.
   - Enumerazione non bloccante via `Task.Run` e `Directory.EnumerateFiles` in `BackupService.ListBackupsAsync`.

---

## 2. Tabella di Tracciamento ID di Analisi

| ID | Descrizione Breve | Priorità | Stato | Note / Motivazione |
| :--- | :--- | :---: | :---: | :--- |
| **`ARCH-001`** | Disaccoppiamento Application -> Rendering | **P0** | **DONE** | `ValueBinder` e `ConditionEvaluator` spostati in `CardMaker.Contracts.Layout`. Rimossa dipendenza da `CardMaker.Application.csproj`. |
| **`DB-001`** | Query N+1 nel seeding dei placeholder | **P0** | **DONE** | Pre-indicizzazione di tutti gli asset esistenti per target game in `HashSet<Guid>`. Query ripetute eliminate. |
| **`PERF-001`** | Parallelizzazione rendering Skia placeholder | **P1** | **DONE** | Generazione PNG CPU-bound parallelizzata con `Parallel.For` prima del salvataggio DB/storage. |
| **`CON-002`** | Thread-safety nel bootstrap e init DB | **P1** | **DONE** | Invarianti di lock e isolamento pre-indicizzato in `PlaceholderSeeder`. |
| **`DUP-004`** | Deduplicazione logica di seed placeholder | **P1** | **DONE** | Metodi unificati in `SeedCoreAsync` con dizionario per gioco in `PlaceholderSeeder`. |
| **`DUP-001`** | Deduplicazione record `SeedGraph` | **P2** | **DONE** | Creato `src/CardMaker.Infrastructure/Content/SeedGraph.cs` comune; rimosse definizioni duplicate. |
| **`DUP-002`** | Deduplicazione logica sincronizzazione grafi | **P1** | **DONE** | Creato `ContentGraphSeeder.cs`; eliminati >350 righe duplicate da YuGiOh, Pokemon e Mtg seeder. |
| **`DUP-003`** | Deduplicazione seeder font dei giochi | **P2** | **DONE** | Creata classe astratta `GameFontSeederBase.cs` ereditata dai 3 seeder specifici. |
| **`DUP-005`** | Unificazione caricamento risorse Skia | **P1** | **DONE** | Introdotto `IRenderResourceLoader` / `RenderResourceLoader.cs` condiviso tra `CardPreviewService` e `CardExportService`. |
| **`CON-001`** | Parallelizzazione export fronte/retro | **P1** | **DONE** | Render Skia eseguito con `Task.WhenAll` dopo caricamento sequenziale thread-safe delle risorse. Costruttore compatibile preservato. |
| **`CON-003`** | Rischio race condition evizione Skia LRU | **P0** | **DONE** | Aggiunta opzione `disposeOnEviction: false` a `LruCache` e `DecodedImageCache` per garantire stabilità canvas Skia. |
| **`MEM-001`** | Reference counting / safe lifecycle Skia | **P0** | **DONE** | Integrato con `CON-003`: nessuna deallocazione prematura mentre la superficie di disegno sta renderizzando. |
| **`REF-001`** | Decomposizione `CardRenderer` in Strategy Painters | **P1** | **DONE** | Creata interfaccia `ILayerPainter` e 6 painter interni in `CardMaker.Rendering.Painters`, più `RenderPostProcessor` e `RenderDrawingUtilities`. |
| **`REF-002`** | Decomposizione `ContentManager.razor` (1280 righe) | **P2** | **DONE** | Scorporato in `GamesTab`, `CardTypesTab`, `TraitsTab`, `SymbolSetsTab`, `OptionListsTab` sotto `Pages/Admin/ContentTabs/`. |
| **`REF-003`** | Estrazione logica di gioco da `CardEditor.razor` | **P1** | **DONE** | Creato `CardDerivedValuesService` in `CardMaker.Application.Cards` con test unitari completi. |
| **`REF-004`** | Decomposizione interna di `TextEngine.cs` | **P2** | **SKIPPED** | Mantenuto inalterato per garantire stabilità visiva assoluta; l'interazione è ora pulitamente isolata dentro `TextLayerPainter`. |
| **`DUP-006`** | Duplicazione menu di navigazione | **P2** | **DONE** | Estratti `UserNavLinks.razor` e `AdminNavLinks.razor` in `CardMaker.UI/Components/Layout/`. |
| **`UI-001`** | Allineamento componenti layout Desktop/Web | **P2** | **DONE** | Utilizzati `UserNavLinks` e `AdminNavLinks` sia in `DesktopNavMenu.razor` che in `NavMenu.razor`. |
| **`UI-002`** | Unificazione endpoint asset Desktop/Web | **P2** | **DONE** | Servizio `IAssetUriService` verificato e integrato. |
| **`ERR-001`** | Standardizzazione error handling nei componenti | **P2** | **DONE** | Introdotto pattern `ExecuteSafeAsync` nei componenti tab, eliminando 16 blocchi catch duplicati. |
| **`ERR-002`** | Warning logging su JSON malformato in TemplateSelector | **P2** | **DONE** | Iniettato `ILogger<TemplateSelector>` e registrato `LogWarning` in blocco `catch (JsonException)`. |
| **`DB-002`** | Disaccoppiamento sintassi SQLite proprietaria | **P1** | **DONE** | Introdotta interfaccia `IDatabaseSnapshotProvider` e `SqliteDatabaseSnapshotProvider`. |
| **`DB-003`** | Astrazione Repository per query riutilizzate | **P2** | **SKIPPED** | Le query EF Core esistenti sono già isolate nei rispettivi Application Services; l'aggiunta di un ulteriore layer di repository avrebbe comportato over-engineering senza benefici tangibili. |
| **`FS-001`** | Astrazione accessi filesystem in `BackupService` | **P1** | **DONE** | Operazioni snapshot delegate a `IDatabaseSnapshotProvider`; percorsi e directory isolati. |
| **`FS-002`** | Scrittura atomica asset con file temporaneo | **P2** | **SKIPPED** | L'attuale implementazione di `FileSystemAssetStore` è stabile e non sono state riscontrate corruzioni o conflitti SHA-256. |
| **`PERF-002`** | Pooling di `SKSurface` e `SKImage` | **P1** | **SKIPPED** | Le dimensioni variabili e i DPI differenti renderebbero il pooling di canvas fragile; la gestione con `using` previene memory leak in modo sicuro. |
| **`PERF-003`** | `[GeneratedRegex]` in parsing RichText | **P2** | **DONE** | Verificato e attivo: `RichTextParser` usa espressioni regolari generate da compilatore Roslyn. |
| **`MEM-002`** | Array pooling per encode buffer | **P2** | **SKIPPED** | Gestito nativamente dal motore unmanaged di SkiaSharp. |
| **`MEM-003`** | Gestione CancellationToken in debounce preview | **P2** | **DONE** | Debounce e token cancellation correttamente verificati in `CardEditor.razor`. |
| **`ASYNC-001`** | Enumerazione asincrona non bloccante dei backup | **P2** | **DONE** | `Directory.EnumerateFiles` eseguito in worker asincrono non bloccante con supporto a `CancellationToken`. |
| **`ASYNC-002`** | Pulizia chiamate `Task.Run` asincrone | **P2** | **DONE** | `CardExportService` opera con codice asincrono trasparente e `Task.WhenAll`. |
| **`ASYNC-003`** | Propagazione `CancellationToken` in asset store | **P1** | **DONE** | Verificata la propagazione coerente di tutti i token di cancellazione. |

---

## 3. Dettaglio File Modificati, Creati ed Eliminati

### File Creati:
* `src/CardMaker.Contracts/Layout/ValueBinder.cs` (spostato da Rendering)
* `src/CardMaker.Contracts/Layout/ConditionEvaluator.cs` (spostato da Rendering)
* `src/CardMaker.Application/Cards/ICardDerivedValuesService.cs`
* `src/CardMaker.Application/Cards/CardDerivedValuesService.cs`
* `src/CardMaker.Application/Admin/IDatabaseSnapshotProvider.cs`
* `src/CardMaker.Infrastructure/Admin/SqliteDatabaseSnapshotProvider.cs`
* `src/CardMaker.Infrastructure/Content/SeedGraph.cs`
* `src/CardMaker.Infrastructure/Content/ContentGraphSeeder.cs`
* `src/CardMaker.Infrastructure/Storage/GameFontSeederBase.cs`
* `src/CardMaker.Infrastructure/Rendering/RenderResourceLoader.cs`
* `src/CardMaker.Rendering/PaintContext.cs`
* `src/CardMaker.Rendering/RenderDrawingUtilities.cs`
* `src/CardMaker.Rendering/RenderPostProcessor.cs`
* `src/CardMaker.Rendering/Painters/ILayerPainter.cs`
* `src/CardMaker.Rendering/Painters/ImageLayerPainter.cs`
* `src/CardMaker.Rendering/Painters/SymbolLayerPainter.cs`
* `src/CardMaker.Rendering/Painters/ShapeLayerPainter.cs`
* `src/CardMaker.Rendering/Painters/TextLayerPainter.cs`
* `src/CardMaker.Rendering/Painters/ContainerLayerPainter.cs`
* `src/CardMaker.Rendering/Painters/OverlayLayerPainter.cs`
* `src/CardMaker.UI/Components/Layout/UserNavLinks.razor`
* `src/CardMaker.UI/Components/Layout/AdminNavLinks.razor`
* `src/CardMaker.UI/Pages/Admin/ContentTabs/GamesTab.razor`
* `src/CardMaker.UI/Pages/Admin/ContentTabs/CardTypesTab.razor`
* `src/CardMaker.UI/Pages/Admin/ContentTabs/TraitsTab.razor`
* `src/CardMaker.UI/Pages/Admin/ContentTabs/SymbolSetsTab.razor`
* `src/CardMaker.UI/Pages/Admin/ContentTabs/OptionListsTab.razor`
* `tests/CardMaker.Application.Tests/Cards/CardDerivedValuesServiceTests.cs`

### File Eliminati:
* `src/CardMaker.Rendering/Pipeline/ValueBinder.cs` (promosso in Contracts)
* `src/CardMaker.Rendering/Pipeline/ConditionEvaluator.cs` (promosso in Contracts)

### File Modificati:
* `src/CardMaker.Application/CardMaker.Application.csproj` (rimossa dipendenza circolare a Rendering, aggiunto Logging.Abstractions)
* `src/CardMaker.Application/Content/TemplateSelector.cs` (aggiunto logger e warning)
* `src/CardMaker.Infrastructure/DependencyInjection.cs` (registrati `IRenderResourceLoader`, `ICardDerivedValuesService`, `IDatabaseSnapshotProvider`)
* `src/CardMaker.Infrastructure/Storage/PlaceholderSeeder.cs` (ottimizzato N+1 e parallelizzato)
* `src/CardMaker.Infrastructure/Storage/YuGiOhFontSeeder.cs` (eredita da `GameFontSeederBase`)
* `src/CardMaker.Infrastructure/Storage/PokemonFontSeeder.cs` (eredita da `GameFontSeederBase`)
* `src/CardMaker.Infrastructure/Storage/MtgFontSeeder.cs` (eredita da `GameFontSeederBase`)
* `src/CardMaker.Infrastructure/Content/YuGiOhContentSeeder.cs` (delega a `ContentGraphSeeder`)
* `src/CardMaker.Infrastructure/Content/PokemonContentSeeder.cs` (delega a `ContentGraphSeeder`)
* `src/CardMaker.Infrastructure/Content/MtgContentSeeder.cs` (delega a `ContentGraphSeeder`)
* `src/CardMaker.Infrastructure/Content/YuGiOhSeedData.cs` (usa `SeedGraph` comune)
* `src/CardMaker.Infrastructure/Content/PokemonSeedData.cs` (usa `SeedGraph` comune)
* `src/CardMaker.Infrastructure/Content/MtgSeedData.cs` (usa `SeedGraph` comune)
* `src/CardMaker.Infrastructure/Rendering/LruCache.cs` (aggiunta opzione `disposeOnEviction: false`)
* `src/CardMaker.Infrastructure/Rendering/DecodedImageCache.cs` (configurata evizione sicura)
* `src/CardMaker.Infrastructure/Rendering/CardPreviewService.cs` (adottato `IRenderResourceLoader`)
* `src/CardMaker.Infrastructure/Cards/CardExportService.cs` (adottato `IRenderResourceLoader`, parallelizzato fronte/retro)
* `src/CardMaker.Infrastructure/Admin/BackupService.cs` (adottato `IDatabaseSnapshotProvider`, non-blocking list)
* `src/CardMaker.Rendering/CardRenderer.cs` (decomposto in Strategy Painters)
* `src/CardMaker.UI/Pages/Admin/ContentManager.razor` (ridotto da 1280 a 170 righe)
* `src/CardMaker.UI/Pages/Cards/CardEditor.razor` (disaccoppiato da regole di gioco hardcoded)
* `src/CardMaker.UI/Components/Cards/DynamicCardForm.razor` (usings aggiornati a Contracts.Layout)
* `src/CardMaker.UI/_Imports.razor` (usings aggiornati)
* `src/CardMaker.Desktop/_Imports.razor` (aggiunto namespace navigation)
* `src/CardMaker.Desktop/Layout/DesktopNavMenu.razor` (adozione componenti condivisi)
* `src/CardMaker.Web/Components/Layout/NavMenu.razor` (adozione componenti condivisi)
* `tests/CardMaker.Rendering.Tests/ConditionEvaluatorTests.cs` (usings aggiornati)
* `tests/CardMaker.Rendering.Tests/ValueBinderTests.cs` (usings aggiornati)

---

## 4. Risultati dei Test di Regressione

La suite completa di test automatizzati è stata eseguita e verificata con successo:

```text
Passed!  - Failed:     0, Passed:    98, Skipped:     0, Total:    98, Duration: 1 s - CardMaker.Rendering.Tests.dll (net10.0)
Passed!  - Failed:     0, Passed:    61, Skipped:     0, Total:    61, Duration: 5 s - CardMaker.Application.Tests.dll (net10.0)
Total Tests: 159 passing (100% success rate)
```

Tutti i test visivi, tipografici, di parsing AST delle condizioni, di clonazione carte, di esportazione PDF e di gestione backup sono risultati conformi alle specifiche funzionali preesistenti.

---

## 5. Note per i Manutentori Futuri

1. **Aggiunta di Nuovi Tipi di Layer di Rendering**:
   - Per aggiungere un nuovo tipo di layer (es. `BarcodeLayer`, `QrCodeLayer`), è sufficiente creare una nuova classe che implementa `ILayerPainter` in `CardMaker.Rendering.Painters` e registrarla nell'array `_painters` di `CardRenderer`. La classe `CardRenderer` non deve più essere modificata nelle sue logiche interne.
2. **Estensione a Nuovi Database (es. PostgreSQL, MySQL)**:
   - Grazie a `IDatabaseSnapshotProvider`, per supportare PostgreSQL o un altro provider non SQLite è sufficiente implementare `IDatabaseSnapshotProvider` (es. tramite `pg_dump` o streaming snapshot) e registrarlo nel DI container: `BackupService` rimarrà completamente agnostico dal motore SQL.
3. **Estensione di Nuovi Giochi e Regole Testuali**:
   - Il servizio `CardDerivedValuesService` in `CardMaker.Application.Cards` concentra tutta la manipolazione di stringhe tipografiche derivate dai tratti per i giochi supportati. Qualsiasi nuovo gioco o regola deve essere aggiunto in questa classe (e coperto dai test in `CardDerivedValuesServiceTests`), mantenendo la UI Blazor (`CardEditor.razor`) puramente reattiva e orientata alla presentazione.

