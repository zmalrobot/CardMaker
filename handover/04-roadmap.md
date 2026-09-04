# 04 — Roadmap

**Legenda stato:** ⬜ da fare · 🟡 in corso · ✅ completata

Ogni fase si chiude con: build verde, test verdi, aggiornamento di `STATE.md` e di questo file.

---

## F0 — Fondamenta ✅

Impalcatura della solution e infrastruttura di base.

- [x] Solution `.NET 10` (`CardMaker.slnx`) con i progetti descritti in `02-architecture.md`
- [x] `Directory.Build.props` (nullable, warnings-as-errors, analyzer), `.editorconfig`, `.gitignore`
- [x] `CardMaker.Domain`: entita' di `03-data-model.md` (17 tabelle + Identity)
- [x] `CardMaker.Infrastructure`: `CardMakerDbContext` EF Core + SQLite, migrazione `InitialSchema`, WAL
- [x] ASP.NET Core Identity con ruoli `Admin` / `User` e amministratore iniziale al primo avvio
- [x] `IAssetStore` content-addressed su filesystem + validazione upload (magic bytes, re-encode, SVG bloccati)
- [x] **Gestione font**: upload, alias di ruolo, anteprima renderizzata, catalogo per gioco
- [x] **Generatore di frame placeholder procedurali** con la geometria della specifica asset
- [x] Host `CardMaker.Web` funzionante: login, libreria asset, font, generazione segnaposto
- [x] Test verdi (validatore upload, geometria, generatore segnaposto, registro font)

**Fatto:** l'app parte, crea l'admin, genera i segnaposto, carica font per ruolo e li rende scaricabili.

---

## F1 — Motore di rendering: nucleo ✅

Il pezzo piu' importante.

- [x] Modelli del layout in `CardMaker.Contracts` + validazione (`LayoutSerializer.Validate`)
- [x] Pipeline `Resolve → Bind → Evaluate → Measure → Paint → Post`
- [x] Valutatore dell'AST condizionale (nessun eval, 14 operatori)
- [x] Layer `staticImage`, `imageSlot` (crop/fit/zoom/offset), `text`, `symbolSlot`, `shape`, `group`
- [x] Motore di testo: misurazione, word wrap, auto-fit (`shrink` / `condense` / `shrinkAndCondense`)
- [x] Stili di testo riutilizzabili con sovrascritture parziali
- [x] Campi calcolati (`join`, `concat`, `count`)
- [x] Caricamento font da byte con risoluzione per alias di ruolo e fallback segnalato
- [x] Angoli arrotondati, gestione abbondanza, export **PNG** e **JPEG**
- [x] Avvisi strutturati del render (asset mancanti, font di ripiego, overflow, artwork a bassa risoluzione)
- [x] `ICardPreviewService`: anteprima server-side senza esporre SkiaSharp alla UI
- [x] Pagina `/admin/render-test` per la verifica interattiva
- [x] 67 test sul motore

**Fatto:** dato un layout e dei valori, il motore produce PNG/JPEG a qualsiasi DPI, con lo stesso
codice per anteprima ed export. Verificato end-to-end dall'interfaccia.

### Rimandato a F2

- Golden image test con diff pixel (per ora la copertura e' su geometria, testo e avvisi)
- Shaping HarfBuzz (serve per legature e scritture non latine, non per l'italiano)
- Export PDF

---

## F2 — Motore di rendering: layer avanzati (copertura Yu-Gi-Oh!) ✅

- [x] `symbolRepeater` (stelle Livello RTL / Rank LTR, max 12)
- [x] `toggleGroup` (8 frecce Link on/off)
- [x] `richText`: parser dei run, simboli inline allineati alla baseline, corsivo/grassetto, bullet
- [x] Sezioni etichettate del testo Rush Duel (`[REQUISITO]` / `[EFFETTO]`) — via marker `richText`, con etichette localizzate a carico del template
- [x] Crop "a fetta" dell'artwork per i **Maximum Monster** Rush (proprietà su `imageSlot`)
- [x] Campi calcolati (`typeLine`, `linkRating`, `maximumAtkLine`) — nessun nuovo operatore necessario, si esprimono con `Join`/`Concat`/`Count` esistenti (ADR-023)
- [x] `overlay` con blend mode e maschera → **foil e rarità**
- [x] Layer `group` e condizionali su tutti i tipi (verificato anche per i layer nuovi di F2)
- [x] Cache LRU degli asset decodificati (per SHA-256); cache dei render completi rimandata a F5
- [x] **Golden image test** con diff pixel e tolleranza (font deterministico Roboto)
- [ ] Shaping HarfBuzz — **deliberatamente rimandato** (ADR-023): nessuna lingua in scope ne ha bisogno
- [x] Export **PDF** (`SKDocument.CreatePdf`), fronte/retro su 2 pagine
- [x] Golden test per i tipi di layer più a rischio (gradiente, ripetitore/toggle, richText)

**Fatto quando:** una carta Xyz, una Link e una Pendulum vengono renderizzate correttamente da JSON
scritto a mano, in PNG/JPG/PDF, con variante foil. **Verificato**: 108 test verdi sul motore.

---

## F3 — Contenuti: modello + seed Yu-Gi-Oh! ✅

- [x] Persistenza di `Game`, `CardType`, `FieldDefinition`, `Template`, `TemplateVersion`,
      `SymbolSet`, `OptionList`, `Trait`
- [x] Versioning dei template (bozza → pubblicata → archiviata; le pubblicate sono immutabili, ADR-007)
- [x] Regole di selezione del template (`ITemplateSelector`, ADR-024)
- [x] Formato pacchetto `.cmpkg` con import/export (protetto da zip-slip, ADR-025)
- [x] Decisione: Rush Duel come `CardType` aggiuntivi nello stesso `Game` (ADR-026)
- [x] Seed Yu-Gi-Oh!: 18 mostri + 3 magie/trappole + 1 token + 1 maximum (3 fette) + 1 skill + 2 retro (28 template totali), set di simboli, liste opzioni, schemi campi

> ⚠️ **Dipendenza esterna:** gli asset grafici **non esistono ancora** e verranno prodotti sulla base
> di [`06-asset-spec.md`](06-asset-spec.md). Fino alla consegna si lavora con i **placeholder
> procedurali** di F0: la struttura è identica, cambia solo l'estetica.

**Fatto:** il gioco Yu-Gi-Oh! esiste nel database, è esportabile e re-importabile identico (verificato con roundtrip test). Tutte le 28 configurazioni di template sono valide e renderizzano senza errori. 121 test verdi complessivi.

---

## F4 — Design system e temi ✅

### Fondamenta

- [x] **Design token** come CSS custom properties (`--cm-*`): colore, spaziatura, raggio, ombra, tipografia
- [x] **Tema chiaro e tema scuro**, entrambi su palette **blu + azzurro** (valori in § Palette)
- [x] Selettore tema: *sistema / chiaro / scuro*, persistito in `localStorage` **e** in cookie (ADR-027, zero FOUC)
- [x] Base tecnica: **Bootstrap 5.3** con `data-bs-theme` + override dei token via CSS variables

### Shell dell'applicazione

- [x] Sidebar responsive collassabile + topbar con indicatore di stato, tema e navigazione
- [x] Intestazioni di pagina curate e badge di stato
- [x] Layout a due colonne riutilizzabile (`TwoColumnLayout.razor`) per "form a sinistra / anteprima a destra"

### Componenti

- [x] Bottoni (primario, accent, secondario, outline, pericolo), campi, select, checkbox, switch
- [x] Tabelle con intestazioni formattate, stato vuoto e densità compatta
- [x] Card, badge, alert, modali
- [x] **Skeleton loader** animato (`SkeletonLoader.razor`) e indicatori di caricamento (`LoadingSpinner.razor`)
- [x] Componente **anteprima carta** (`CardPreview.razor`): fondo a scacchiera per la trasparenza (neutro, mai blu), zoom (50%, 100%, 150%, Fit), pannello avvisi del motore

### Qualità

- [x] Contrasto **WCAG AA** verificato su entrambi i temi
- [x] Focus visibile (`--cm-focus-ring`), navigazione da tastiera, `prefers-reduced-motion`
- [x] Responsive reale su 360 px, 768 px, 1280 px, 1920 px
- [x] Pagina `/design` con la galleria dei componenti, usata come riferimento e collaudo visivo

### Palette

Blu come colore primario, azzurro come accento. Valori implementati in `cardmaker-theme.css`:

| Token | Tema chiaro | Tema scuro |
|---|---|---|
| `--cm-bg` | `#F6F9FF` | `#0A1020` |
| `--cm-surface` | `#FFFFFF` | `#121A2C` |
| `--cm-surface-2` | `#EDF3FF` | `#1A2438` |
| `--cm-border` | `#D6E2F7` | `#26324A` |
| `--cm-text` | `#0F1B2D` | `#E8EEF9` |
| `--cm-text-muted` | `#52627A` | `#9AA9C0` |
| `--cm-primary` | `#1D5FD8` | `#4C8DFF` |
| `--cm-primary-hover` | `#1A54C0` | `#6BA1FF` |
| `--cm-accent` (azzurro) | `#29A9E8` | `#4FC3F7` |
| `--cm-success` | `#1E9E6A` | `#35C08A` |
| `--cm-warning` | `#C77700` | `#E0A030` |
| `--cm-danger` | `#C8382F` | `#F2645A` |

> ⚠️ Il **fondo dell'anteprima carta** non è mai blu: usa scacchiera neutra `--cm-checkerboard-1/2`.

**Fatto:** esiste `/design` con tutti i componenti nei due temi, il tema si cambia senza lampeggiamenti (SSR + cookie) e tutte le pagine admin esistenti sono state aggiornate sul nuovo sistema.

---

## F5 — Flusso utente ✅

- [x] Wizard: gioco → tipo carta → variante → traits → campi
- [x] **Form dinamico** generato da `FieldDefinition` (tutti i `Kind` e `VisibleWhen` condizionale)
- [x] Upload artwork con avviso di risoluzione insufficiente
- [x] Editor del testo effetto (`RichTextEditor.razor`) con inserimento simboli e formattazione
- [x] **Anteprima live** server-side (debounce 200 ms, cancellazione token, selezione automatica template)
- [x] Salvataggio carte, "Le mie carte" (`MyCards.razor`), riapertura, duplicazione, eliminazione
- [x] Export multiformato (`ExportModal.razor` & `CardExportService.cs`): PNG / JPG / PDF a 600 DPI, fronte e/o retro (2 pagine su PDF), con/senza abbondanza

**Fatto:** un utente crea, salva, riapre, modifica, duplica ed esporta una carta Yu-Gi-Oh! completa in alta risoluzione (125 test verdi).

---

## F6 — Admin: gestione contenuti ✅

- [x] Asset library: upload multiplo (`multiple`), ricerca/filtro, anteprima, sostituzione blob preservando ID, cancellazione sicura con blocco se in uso (`CheckAssetUsageAsync`), campi licenza/provenienza
- [x] Gestione font estesa (anteprima comparativa, sostituzione ruolo e riassegnazione)
- [x] CRUD giochi, tipi di carta, traits, set di simboli, liste opzioni (`/admin/content`)
- [x] **Editor dello schema campi** (`/admin/schema/{id}`) con anteprima interattiva live del form risultante
- [x] Audit log delle azioni admin (`/admin/audit` & `AuditLogEntry`)

**Fatto:** un admin crea un gioco nuovo da zero (es. Pokémon TCG), configura tipi di carta, tratti, simboli, opzioni e schema campi senza toccare il codice o il database (130 test verdi).

---

## F7 — Admin: editor template WYSIWYG ✅

La fase più costosa in termini di UI.

- [x] Canvas con anteprima server-side + overlay DOM e highlight bounding box
- [x] Pannello layer (albero, z-order, visibilità, blocco, rinomina, duplica)
- [x] Pannello proprietà per ogni tipo di layer
- [x] Griglia, snap, guide di sicurezza (Safe Zone 3 mm) e di abbondanza (Bleed 2 mm), zoom (50%-150%)
- [x] Undo/redo
- [x] **Builder visuale delle regole condizionali (`VisibleWhen`)**
- [x] Anteprima live con dati campione e debounce
- [x] Validazione statica: layer fuori dai limiti, binding a campi inesistenti, asset/font non registrati
- [x] Gestione versioni: bozze incrementali, pubblica versione, changelog e note di modifica

**Fatto quando:** un template Yu-Gi-Oh! viene ricostruito da zero interamente nell'editor.

---

## F8 — Host desktop (Windows, macOS, Linux) ✅

- [x] Progetto `CardMaker.Desktop` su **Photino.Blazor** che riusa la Razor Class Library
- [x] Database e asset nella cartella dati di sistema per ciascun OS
      (`%LOCALAPPDATA%` · `~/Library/Application Support` · `~/.local/share` / `$XDG_DATA_HOME`)
- [x] **Bypass admin locale** (confinato all'assembly desktop in-process, nessun listener di rete)
- [x] Inizializzazione e import automatico dei segnaposto e di Yu-Gi-Oh! al primo avvio
- [x] Pubblicazione self-contained verificata (es. `linux-x64`, `win-x64`, `osx-x64`)
- [x] Supporto WebView di sistema native: WebView2 (Win), WKWebView (mac), WebKitGTK (Linux)

**Fatto quando:** l'eseguibile desktop funziona offline sui tre sistemi operativi con le stesse
funzionalità del web.

---

## F9 — Hardening e messa in produzione ✅

Richiesta esplicita: **la web app sarà esposta su internet**.

- [x] Registrazione **solo su invito** con token crittografici a scadenza e blocco fail-closed
- [x] HTTPS/HSTS, CSP e security headers (`X-Frame-Options`, `nosniff`, `Referrer-Policy`), antiforgery
- [x] Rate limiting sliding window su chiamate web e protezione da DoS su rendering/login
- [x] SQLite in WAL mode con snapshot atomici online via `VACUUM INTO` e verifica integrità (`PRAGMA integrity_check`)
- [x] Containerizzazione Docker multi-stage (SkiaSharp native C libs) + reverse proxy Caddy con TLS automatico
- [x] Logging strutturato, audit log amministrativo, endpoint `/healthz`
- [x] Scansione automatica dipendenze vulnerabili in CI (`.github/workflows/security.yml`)

**Fatto quando:** la checklist di [`02-architecture.md` § 9.2](02-architecture.md) è completa e il
ripristino da backup è stato provato davvero.

---

## F10 — Rifiniture finali ✅

- [x] Localizzazione completa IT/EN (UI + contenuti + pagine Identity in italiano)
- [x] Gestione utenti e inviti da parte dell'admin (`/admin/invitations`)
- [x] Backup/ripristino del database e degli asset dall'interfaccia (`/admin/backups`)
- [x] Pagina ToS/disclaimer "fan-made, non in vendita, non affiliato" (`/disclaimer` e `/terms`)
- [x] Componente `AppFooter` con badge non commerciale e collegamenti legali
- [x] Telemetria di errore locale e logging strutturato verificati

**Fatto quando:** la v1 è completa in ogni dettaglio per Yu-Gi-Oh! (classico + Rush Duel),
ha superato tutti i test, è bilingue e pronta per essere provata dagli utenti.

---

# v2 — Gli altri due giochi

> **Verifica del principio architetturale (ADR-001).** Se F11 e F12 richiedono modifiche al motore
> che vanno oltre l'aggiunta di nuovi *tipi di layer*, il design data-driven ha fallito.
> Rush Duel (F2-F3) è il primo test, più economico: non deve richiedere alcun nuovo tipo di layer.

---

## F11 — Pokémon TCG ⬜

Formato **63 × 88 mm** → a 600 DPI: trim **1488 × 2079 px**, master con 2 mm di abbondanza
**1582 × 2173 px**. Verificare con `CardGeometry.PokerSize()`, che è già implementata e testata.

### La difficoltà vera: il layout a flusso verticale

È l'unica capacità che il motore **non ha**. Su una carta Pokémon il numero di attacchi (1–3) e la
lunghezza dei testi ridistribuiscono lo spazio: non basta il posizionamento assoluto.

- [ ] Nuovo layer **`repeatingBlock`**
  - sorgente: un campo lista (es. `attacks`), ogni elemento è un dizionario di sotto-campi
  - `itemTemplate`: sotto-layer con coordinate **relative al blocco**, non alla carta
  - altezza dell'item calcolata dal contenuto (il testo dell'attacco manda a capo)
  - `distribute`: `start | center | spaceBetween | spaceAround`
  - `gap`, `minHeight`, `maxItems`
**1582 × 2173 px**.

- [x] Nuovo layer **`repeatingBlock`** (misura ricorsiva, auto-fit, `distribute` stile flex)
- [x] **Simboli inline nel testo** (costo energia dentro il testo dell'attacco) → implementato tramite `richText` e simboli procedurali `{sym:energies.*}`
- [x] **Orientamento orizzontale**: `CardOrientation.Landscape` pienamente supportato
- [x] **Bolla di evoluzione**: slot con maschera e posizionamento calibrato
- [x] **Riga Weakness / Resistance / Retreat**: combinazioni simbolo + moltiplicatore testuale supportate
- [x] **Rule box** (testo regola di V/ex/GX): `text` condizionale
- [x] Notazione del danno con suffissi `+`, `×`, `-`: campo testo libero
- [x] **Font incorporati**: `GillSansBold`, `GillSansItalic`, `GillSans`, `Futura-Bold` con seeder e mapping automatico

**Fatto:** carte Pokémon generate a 150/300/600 DPI, con allineamenti millimetrici verificati visivamente e tramite test.

---

## F12 — Magic: The Gathering ✅

Formato **63 × 88 mm**, raggio angoli **3.18 mm**.

- [x] **Parser dei token di mana e simboli procedurali SkiaSharp**:
  - Colori base: `{W}`, `{U}`, `{B}`, `{R}`, `{G}`, `{C}`
  - Generici e variabili: numeri `{0}`–`{9}`, `{X}`
  - Azioni e tap: `{T}` (Tap)
  - Simboli di rarità procedurali: Comune, Non comune, Rara, Mitica
- [x] **Allineamento alla baseline e centraggio ottico `CapHeight`**: calibrazione millimetrica per evitare che font con ascendenti marcate (come Beleren) tocchino i bordi
- [x] **Font incorporati**: `Beleren2016-Bold`, `Beleren2016SmallCaps-Bold`, `Mplantin`
- [x] **Contenuti seminati**:
  - **Tipi:** Creatura, Planeswalker, Istantaneo, Stregoneria, Incantesimo, Artefatto, Terra
  - **Frame e colori:** White, Blue, Black, Red, Green, Multicolor, Colorless, Artifact, Land
  - **Ruoli font:** `mtg-card-name`, `mtg-type-line`, `mtg-rules-text`, `mtg-flavor-text`, `mtg-power-toughness`, `mtg-loyalty`, `mtg-artist`, `mtg-collector-number`
  - **Campi:** name, manaCost, artwork, supertypes, cardTypes, subtypes, rulesText, flavorText, power, toughness, loyalty, artist, collectorNumber, setCode, rarity

**Fatto:** carte MTG renderizzate con fedeltà tipografica elevata a 150/300/600 DPI, simboli inline nel testo regole e calibrazione perfetta degli offset.

---

## F13 — Rifinitura UX, 60 FPS Asincrono & Logging Pulito ✅

- [x] **Offload Asincrono SkiaSharp / SQLite**: incapsulamento completo in `Task.Run(...)` per preview, export e PDF, garantendo un thread UI Blazor/Photino sempre libero a 60 FPS (ADR-037).
- [x] **Accelerazione Hardware GPU**: rimozione del costoso `backdrop-filter: blur` in WebKitGTK Linux e introduzione di `will-change: transform` e `transform: translateZ(0)` sui caricatori CSS.
- [x] **Correzione NavMenu**: impostazione di `Match="NavLinkMatch.All"` su `cards` sia su Desktop che Web per impedire la contemporanea selezione di "Le mie carte" e "Nuova carta".
- [x] **Azzeramento Spam Base64 in Console (ADR-036)**: `SetLogVerbosity(0)` in `CardMaker.Desktop/Program.cs` per disattivare l'echo nativo dei messaggi IPC WebKitGTK.
- [x] **Logging Strutturato Sintetico**: introduzione di `ILogger<T>` su `CardPreviewService`, `CardExportService`, `CardService` e `AssetService` con log informativi compatti (`[Preview]`, `[Export]`, `[Card]`, `[Asset]`).

---

## Sintesi: Capacità del Motore per i Tre Giochi

| Capacità | Stato | Gioco |
|---|---|---|
| `staticImage`, `imageSlot`, `text`, `symbolSlot`, `shape`, `group` | ✅ F1 | Tutti |
| Auto-fit con compressione orizzontale (`shrinkAndCondense`) | ✅ F1 | Tutti (spec. YGO) |
| Condizioni (`VisibleWhen`), campi calcolati, stili di testo | ✅ F1 | Tutti |
| Font per ruolo con fallback e risorse embedded | ✅ F1/F11/F12 | Tutti |
| `symbolRepeater`, `toggleGroup` | ✅ F2 | YGO (Livello/Rank, Link) |
| `richText` con simboli inline `{sym:...}` | ✅ F2 | Tutti |
| `overlay` foil, blend, maschere | ✅ F2 | Rarità di tutti i giochi |
| Export raster PNG/JPG e vettoriale PDF (fronte/retro) | ✅ F2 | Tutti |
| Crop "a fetta" per Maximum Monster | ✅ F2 | YGO Rush Duel |
| Orientamento orizzontale (Landscape) | ✅ F11 | Tutti |
| Simboli procedurali energia SkiaSharp | ✅ F11 | Pokémon TCG |
| Simboli procedurali mana & rarità SkiaSharp | ✅ F12 | Magic: The Gathering |
| Centraggio ottico verticale su `CapHeight` | ✅ F12 | Tutti (Beleren, GillSans, Stone Serif) |
| Mappatura 1:1 Full-Bleed Master Canvas (zero distorsione) | ✅ F12 | Tutti |
| Pipeline asincrona 60 FPS & Logging minimale senza base64 | ✅ F13 | Desktop & Web |

> Sono **6 tipi di layer nuovi in tutto** per coprire tre giochi. Se il numero cresce molto oltre,
> è il segnale che il modello di layout va ripensato.

---

## Rischi noti

| Rischio | Impatto | Mitigazione |
|---|---|---|
| Gli asset grafici non esistono ancora | Blocca F3/F5 | **Placeholder procedurali** costruiti in F0 sulle misure di `06-asset-spec.md`; la sostituzione con gli asset reali è un semplice re-upload |
| L'auto-fit del testo non convince visivamente | Alto — è ciò che rende la carta credibile | Affrontato in F1; golden test e confronto visivo in F2 |
| L'editor WYSIWYG si dilata | Alto | F6 fornisce già un editor a form: l'app è usabile anche senza F7 |
| Performance a 600 DPI | Medio | Cache, coda limitata, anteprima a DPI ridotto |
| WebKitGTK su Linux: versioni e distribuzioni eterogenee | Medio | Testare su Ubuntu LTS; documentare le dipendenze; fallback "server locale + browser di sistema" |
| App pubblica su internet: abuso del renderer | Medio | Rate limiting, quote, coda; registrazione a invito |
| Il `repeatingBlock` è il pezzo più complesso rimasto | Medio | Affrontarlo in F11 con test dedicati, non insieme ad altro |
