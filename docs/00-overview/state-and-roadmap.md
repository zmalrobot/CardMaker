# Stato del Progetto e Roadmap

> **Stato Corrente**: **v2 & Suite AI Multimodale Completate al 100% (Fasi F0 → F15)**  
> **Test Suite**: **256 test automatici (104 rendering + 152 applicativi e AI)** — 100% passati, 0 errori, 0 avvisi.  
> **Piattaforme**: Desktop nativo (Photino.Blazor) e Web (ASP.NET Core Kestrel) operativi e verificati.

---

## 1. Stato di Avanzamento per Fasi

| Fase | Denominazione | Obiettivo e Contenuti | Stato |
|---|---|---|:---:|
| **F0** | **Fondamenta** | Solution .NET 10 (`CardMaker.slnx`), Clean Architecture, SQLite WAL, Identity, asset store SHA-256, catalogo font, generatori segnaposto procedurali. | ✅ Completata |
| **F1** | **Motore di Rendering Core** | Pipeline di rendering data-driven SkiaSharp a 6 fasi, coordinate normalizzate 0..1, `TextEngine` con auto-fit tipografico (*shrink*, *condense*), export PNG/JPEG. | ✅ Completata |
| **F2** | **Layer Avanzati** | Ripetitori di simboli, frecce Link per Yu-Gi-Oh!, rich text con simboli inline `{sym:...}`, layer texture foil, esportatore vettoriale PDF, golden test visivi. | ✅ Completata |
| **F3** | **Contenuti Yu-Gi-Oh! & Rush Duel** | Grafo di dominio completo (26 tipi carta, 28 template), selettore template dinamico, formato pacchetto esportabile `.cmpkg` con protezione anti-zip-slip. | ✅ Completata |
| **F4** | **Design System & Temi** | Token CSS su palette blu/azzurro chiaro e scuro (`data-bs-theme`), layout responsive, componenti condivisi (`CardPreview`, `ThemeToggle`, `SkeletonLoader`). | ✅ Completata |
| **F5** | **Flusso Utente Carte** | Wizard di creazione con form dinamico generato da `FieldDefinition`, anteprima live debouncata a 60 FPS, collezione "Le mie carte", duplicazione ed export a 600 DPI. | ✅ Completata |
| **F6** | **Admin: Gestione Contenuti** | CRUD completo giochi, tipi carta, tratti, simboli, opzioni (`/admin/content`), editor dello schema campi (`/admin/schema/{id}`), upload asset con filtri e safe delete. | ✅ Completata |
| **F7** | **Admin: Template Studio WYSIWYG** | Studio a 3 pannelli (`/admin/templates/{id}`), albero layer con z-order, viewport interattiva con guide Bleed (2 mm) e Safe Zone (3 mm), visual condition builder `VisibleWhen`. | ✅ Completata |
| **F8** | **Host Desktop Cross-Platform** | Shell desktop nativa `CardMaker.Desktop` su **Photino.Blazor** (Linux, Windows, macOS), percorsi di sistema standard, bypass admin locale offline (ADR-009, ADR-031). | ✅ Completata |
| **F9** | **Hardening & Produzione** | Registrazione a invito con token crittografici SHA-256 monouso, Content Security Policy restrittiva, rate limiting, snapshot online SQLite (`VACUUM INTO`), Docker e Caddy. | ✅ Completata |
| **F10** | **Rifiniture & Localizzazione** | Pagine Identity localizzate in italiano, note legali e disclaimer fan-made (`/disclaimer`), footer applicativo calibrato. | ✅ Completata |
| **F11** | **v2 — Pokémon TCG** | Supporto Poker Size (63 × 88 mm), seeding completo (Base, Fasi 1-2, EX, GX, V, VMAX, Trainer, Energie), simboli procedurali energia e font incorporati. | ✅ Completata |
| **F12** | **v2 — Magic: The Gathering** | Supporto Poker Size, seeding completo (Creature, Planeswalker, Istantanei, Stregonerie, Incantesimi, Artefatti, Terre), simboli procedurali di mana ed elisir rarità. | ✅ Completata |
| **F13** | **UX 60 FPS, Refactoring & Pulizia** | Decomposizione di `CardRenderer` in Strategy Painters, disaccoppiamento `Application -> Rendering`, offload asincrono `Task.Run`, azzeramento rumore IPC console (`SetLogVerbosity(0)`). | ✅ Completata |
| **F14** | **Motore AI Locale — Testo** | Motore di inferenza on-device `llama.cpp` / LLamaSharp (modelli Gemma 2/3), rilevamento RAM e selezione hardware, download asincrono con HTTP Range resume all'avvio, banner UI live, versione motore in footer. | ✅ Completata |
| **F15** | **Motore AI Locale — Generazione Immagini** | Generazione locale text-to-image con **Stable Diffusion GGUF** (SD 1.5 Turbo, DreamShaper 8, SDXL Lightning), modale `AiImageGenerationModal`, preset artistici TCG, seed personalizzato, download concorrente all'avvio, salvataggio automatico nello store asset SHA-256 e binding immediato all'artwork della carta. | ✅ Completata |

---

## 2. Dettaglio Risultati Recenti

### Generazione Immagini ed Artwork con AI Locale (CardMaker.AI)
- Integrazione di `IImageGenerationEngine` e implementazione del motore di diffusione nativo GGUF/SkiaSharp.
- Profili modelli dedicati nel registro centrale: **SD 1.5 Turbo** (Q8_0, 4 GB RAM), **DreamShaper 8 SD 1.5 LCM** (IQ4_NL, 8 GB RAM) e **SDXL Lightning 4-Step** (Q4_0, 16 GB RAM).
- Modale dedicato `AiImageGenerationModal.razor` con selezione rapida di stili TCG (*Fantasy*, *Anime*, *Dark Fantasy*, *Cyberpunk*, *Pittura ad Olio*, *Acquerello*), negative prompt automatico e anteprima in tempo reale.
- Persistenza automatica dell'opera generata come file PNG nello store content-addressed via `IAssetCatalog` e binding immediato all'anteprima 60 FPS della carta.
- Download automatico dei modelli immagini integrato in `AiModelManager.EnsureAllActiveModelsReadyAsync` con supporto HTTP Range resume, monitoraggio in UI e footer con indicazione multimodale.
- Suite di test estesa a **256 test automatici totali** (104 rendering + 152 applicativi/AI), tutti superati con 0 errori e 0 warning.

### Motore AI Testuale (CardMaker.AI)
- Registri modelli Gemma (2B, 4B, 9B, 27B), binding `llama.cpp` ed esecuzione su CPU senza overhead continuo (0 MB a riposo).
- `AiModelDownloader` con download in streaming HTTP, ripresa da offset parziale (`Range: bytes={x}-`), validazione preventiva spazio su disco (`DriveInfo`), retry con backoff e controllo integrità magic bytes GGUF.
- Coordinatore `AiModelManager` per verifica non bloccante allo startup, notifica eventi di progresso in tempo reale e cancellazione reattiva.
- Popolamento intelligente ed esaustivo dei form: titolo, descrizione, tipo coerente e statistiche ATK/DEF.

### Decomposizione Architetturale
- `CardRenderer.cs` scorporato da una classe monolitica di oltre 1000 righe in un'architettura modulare a Strategy Painters (`ILayerPainter`), con moduli separati `RenderPostProcessor` e `RenderDrawingUtilities`.
- `ContentManager.razor` decomposto in 5 tab indipendenti in `Pages/Admin/ContentTabs/`.
- Logica di estrazione dei valori derivati delle carte incapsulata in `CardDerivedValuesService`.

### Ottimizzazione e Concorrenza
- Seeding dei placeholder parallelizzato tramite `Parallel.For` con eliminazione delle query N+1.
- Rendering ed esportazione PDF fronte/retro parallelizzati via `Task.WhenAll`.
- Superfici SkiaSharp protette da deallocazioni premature nella cache LRU (`disposeOnEviction: false`).
- Test suite espansa e robustita fino a **200 test automatizzati** su xUnit.

---

## 3. Roadmap e Direzioni Future

Benché tutte le fasi pianificate della v2 siano pienamente operative, la struttura attuale è predisposta per accogliere i seguenti sviluppi evolutivi:

1. **Pacchetti Asset Artistici**:
   - Sostituzione facoltativa dei frame e simboli procedurali con asset grafici disegnati da illustratori, secondo le specifiche in [`docs/10-reference/asset-spec.md`](../10-reference/asset-spec.md).
2. **Estensione a Nuovi TCG**:
   - Integrazione di giochi addizionali (es. *One Piece Card Game*, *Disney Lorcana*) sfruttando il pattern unificato `ContentGraphSeeder` + font embedding + layout template senza modifiche al codice del motore.
3. **Distribuzione Desktop Nativa Confezionata**:
   - ✅ **Implementato**: Installer Windows Inno Setup (`.exe`), pacchetto nativo Debian/Ubuntu (`.deb`) e archivi portatili (Windows/Linux) integrati nella pipeline GitHub Actions [`.github/workflows/release.yml`](../../.github/workflows/release.yml).
   - *Estensioni future*: Bundle firmati per macOS (`.dmg`) o pacchetti universali AppImage / MSIX.
4. **Generazione Batch (Opzionale)**:
   - Importazione di record multipli da CSV per la generazione massiva di mazzi completi.
