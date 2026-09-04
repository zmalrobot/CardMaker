# Glossario

Termini tecnici, abbreviazioni e gergo del progetto CardMaker.

---

## A

**Abbondanza** → Bleed

**ADR** (Architectural Decision Record)
: Registro di una decisione architetturale. Formato: contesto → decisione → conseguenze. Le decisioni superate non si cancellano, si marcano. Vedi `docs/09-decisions/`.

**AST** (Abstract Syntax Tree)
: Struttura dati ad albero che rappresenta le condizioni logiche dei layer (`VisibleWhen`) e delle regole di selezione template (`SelectionRuleJson`). Implementato come JSON tipizzato; nessun `eval` (ADR-006).

**Asset**
: Qualsiasi file binario caricato dall'admin: frame PNG, simbolo PNG, texture foil, font TTF/OTF. Identificato da un GUID nel database; il file fisico ha nome = SHA-256 del contenuto (ADR-005).

---

## B

**Bleed** (Abbondanza tipografica)
: Area aggiuntiva attorno al trim (2 mm = 47 px @600 DPI). La grafica deve estendersi fino al bordo del master canvas, così il ritaglio non lascia bordi bianchi. Il motore applica le maschere degli angoli arrotondati sull'area di trim.

**Binding**
: Riferimento di un layer a un campo della carta tramite `{{nomeField}}` (nei template di testo) o `Source = FieldKey` (negli slot immagine/simbolo). Risolto da `ValueBinder` al momento del render.

**Bootstrap 5.3**
: Framework CSS base del design system. I token `--cm-*` estendono i token nativi di Bootstrap. Vedi ADR-027.

---

## C

**CapHeight**
: Altezza delle lettere maiuscole nel font corrente, usata per il centraggio ottico verticale del testo (ADR-035). Formula: `metrics.CapHeight > 0 ? metrics.CapHeight : -metrics.Ascent * 0.7f`.

**CardGeometry**
: Classe in `CardMaker.Contracts` che calcola le misure di un canvas carta: master, trim, bleed, safe zone, DPI. È l'unica fonte di verità per le misure; verificata da `CardGeometryTests`.

**CardLayout**
: Documento JSON che descrive i layer di un template. Deserializzato in `CardMaker.Contracts.Layout` tramite polimorfismo sul campo `type`. Versioni pubblicate sono immutabili (ADR-007).

**CardType**
: Tipo di carta dentro un gioco (es. `monster-effect`, `spell`, `rush-trap`). Contiene la lista di `FieldDefinition` e punta a uno o più `Template`.

**Clean Architecture**
: Schema architetturale adottato dal progetto. Le dipendenze puntano verso l'interno: `Desktop`/`Web` → `UI` → `Application` → `Domain`/`Contracts`. `Rendering` e `Infrastructure` implementano le porte di `Application`. Vedi ADR-001 e `docs/01-architecture/architecture.md`.

**`.cmpkg`**
: Formato di pacchetto ZIP per export/import di giochi completi tra istanze. Contiene `manifest.json`, `game.json` e i binari degli asset in `assets/{sha256}`. Protezione Zip-Slip integrata (ADR-025).

**Condition / ConditionEvaluator**
: AST JSON usato per `VisibleWhen` sui layer e `SelectionRuleJson` sui template. Valutato da `ConditionEvaluator` con i valori della carta corrente. Operatori fissi e chiusi (ADR-006).

**Content-addressed storage**
: Schema di storage dove il nome del file è determinato dal suo contenuto (SHA-256). Deduplicazione automatica, sicurezza path traversal, cache HTTP immutabile (ADR-005).

---

## D

**DPI** (Dots Per Inch)
: Risoluzione di rendering. Il progetto usa:
- **150 DPI** — anteprima rapida (preview panel)
- **300 DPI** — export qualità media
- **600 DPI** — export alta definizione (master)

**Debounce**
: Ritardo (200 ms) applicato prima di inviare una richiesta di render a seguito di modifiche dell'utente. Evita di sovraccaricare il server mentre l'utente digita. Le richieste intermedie vengono annullate con `CancellationToken`.

---

## F

**FieldDefinition**
: Metadato di un campo di una carta (nome, tipo, default, `VisibleWhen`, ordine). Definisce il form dinamico mostrato all'utente. I valori compilati sono serializzati in JSON e salvati in `Card.ValuesJson`.

**FieldKind**
: Tipo del campo di una carta: `Text`, `Number`, `Select`, `Image`, `Toggle`, `List`, ecc. Determina il controllo UI generato da `DynamicCardForm`.

**FOUC** (Flash of Unstyled Content)
: Lampeggio visivo durante il caricamento della pagina, prima che il CSS venga applicato. Prevenuto con script inline sincrono nel `<head>` che applica il tema (ADR-027).

**FontService**
: Servizio che risolve gli alias di ruolo font (`card-name`, `effect`, ecc.) nei file fisici caricati dall'admin. Con fallback a Roboto per evitare crash al render.

**Full-bleed**
: Si dice di un'immagine che copre l'intera area master (1488 × 2125 px). Il motore la disegna direttamente sul canvas master senza scalare, garantendo l'allineamento pixel-perfect (ADR-035).

---

## G

**Game** (gioco)
: Entità database che rappresenta un gioco di carte: `yugioh`, `pokemon`, `mtg`. Ha una `CardGeometry` associata (dimensioni fisiche e DPI).

**Golden test**
: Test di regressione visiva che confronta un'immagine renderizzata con un'immagine di riferimento salvata nel repository. Tolleranza: 12/255 per canale, max 1% di pixel diversi.

---

## I

**IDecodedImageCache**
: Cache LRU singleton che memorizza le `SKImage` decodificate, chiave = SHA-256. Evita di ridecodificare lo stesso asset a ogni render. Le immagini "in prestito" dalla cache non vengono disposte dalla singola richiesta (ADR-023).

**IFileDownloadService**
: Interfaccia (pianificata, non ancora implementata) per il download di file, con due implementazioni: `WebFileDownloadService` (blob URL JS) e `DesktopFileDownloadService` (dialogo GTK nativo). Vedi `docs/08-operations/troubleshooting.md`.

**ITemplateSelector**
: Interfaccia che valuta i template di un `CardType` in ordine di `SortOrder` e restituisce quello la cui `SelectionRuleJson` è soddisfatta dai valori della carta corrente (ADR-024).

---

## L

**Layer**
: Elemento grafico di un template. Tipi polimorfici: `staticImage`, `imageSlot`, `text`, `richText`, `symbolSlot`, `symbolRepeater`, `toggleGroup`, `shape`, `overlay`. Le posizioni sono in coordinate normalizzate 0..1 (ADR-008).

**LOH** (Large Object Heap)
: Segmento del garbage collector .NET per oggetti > 85 KB. Le bitmap SkiaSharp rientrano in questa categoria. Le ottimizzazioni di performance mirano a ridurre le allocazioni LOH inutili.

---

## M

**Master canvas**
: Canvas a piena abbondanza sul quale il motore disegna. Per Yu-Gi-Oh!: 1488 × 2125 px @600 DPI. Il trim (carta ritagliata) è 1394 × 2031 px, centrato nel master con offset (47, 47).

**mm → px formula**

$$\text{px} = \left\lfloor \frac{\text{mm} \times \text{DPI}}{25.4} + 0.5 \right\rfloor$$

A 600 DPI: $1\text{ mm} \approx 23.622\text{ px}$

---

## N

**NormalizedRect**
: Struttura `(X, Y, W, H)` dove tutti i valori sono in `[0, 1]` relativi alle dimensioni del trim (ADR-008). Per `toggleGroup`, relativo al rettangolo del gruppo.

---

## P

**Painter**
: Classe che implementa la strategia di rendering per un tipo di layer (es. `StaticImagePainter`, `TextPainter`, `SymbolRepeaterPainter`). Tutte implementano `ILayerPainter`.

**Photino.Blazor**
: Framework per host desktop che usa la WebView nativa del SO (WebKitGTK su Linux, WebView2 su Windows, WKWebView su macOS). Permette di riusare i componenti Blazor senza un server HTTP (ADR-011).

**PreloadedRenderResources**
: Classe che carica prima del render tutti gli asset necessari (font, immagini). Distingue risorse possedute (da disporre a fine render) e risorse in prestito dalla cache (da non disporre).

---

## R

**Rendering pipeline**
: Sequenza in 6 fasi: Pre → Template resolve → Resource load → Paint → Post → Encode. Vedi `docs/04-application/rendering-engine.md`.

**richText**
: Tipo di layer per testo misto (grassetto, corsivo, simboli inline, sezioni etichettate, elenchi puntati). Senza auto-fit: dimensione fissa con word-wrap. Markup: `**bold**`, `*italic*`, `{sym:set.key}`, `[LABEL]`, `- bullet` (ADR-023).

---

## S

**Safe zone**
: Area interna al trim (margine 3 mm) entro cui devono stare tutti i testi e simboli critici. Per Yu-Gi-Oh! @600 DPI: 1252 × 1889 px, origine (118, 118).

**SHA-256**
: Hash crittografico usato come nome file degli asset (content-addressed storage). Garantisce deduplicazione, integrità e assenza di path traversal (ADR-005).

**SkiaSharp**
: Binding .NET della libreria grafica 2D Skia (Google). Usato per rasterizzazione, testo, blend mode, maschere, gradienti e generazione PDF (ADR-002).

**sRGB**
: Spazio colore standard usato in tutta la pipeline. Nessun CMYK, nessun profilo ICC personalizzato (ADR-013).

---

## T

**TCG** (Trading Card Game)
: Gioco di carte collezionabili. I tre giochi target del progetto sono Yu-Gi-Oh!, Pokémon TCG e Magic: The Gathering.

**Template**
: Combinazione di un `CardType` con un `CardLayout` JSON. Una carta punta a una `TemplateVersion` specifica; le versioni pubblicate sono immutabili (ADR-007).

**TextEngine**
: Componente del motore di rendering che gestisce il posizionamento del testo: auto-fit (shrink, condense), word-wrap, rich text, centraggio ottico CapHeight. Vedi `docs/04-application/rendering-engine.md`.

**Trim**
: Bordo di taglio della carta stampata. Dimensione fisica dell'output finale (es. 59 × 86 mm per Yu-Gi-Oh!). Centrato nel master canvas con un margine di bleed su tutti i lati.

---

## V

**VACUUM INTO**
: Comando SQLite che produce una copia transazionalmente coerente del database senza lock bloccanti. Usato da `BackupService` per i backup online sicuri (ADR-032).

**ValueBinder**
: Componente che risolve i `{{fieldKey}}` nei template di testo usando i valori della carta corrente (`Card.ValuesJson`). Usato anche da `ConditionEvaluator`.

**VisibleWhen**
: Campo opzionale di un `FieldDefinition` o di un layer, contiene un `Condition` JSON. Il layer o il campo vengono mostrati solo se la condizione è verificata sui valori attuali della carta.

---

## W

**WAL** (Write-Ahead Log)
: Modalità SQLite che permette letture concorrenti durante le scritture. Attivata globalmente via `PRAGMA journal_mode=WAL`. Riduce la latenza e aumenta la concorrenza.

**WebKitGTK**
: Implementazione open-source di WebKit per Linux/GTK. La WebView usata da Photino su Linux. Non supporta il pattern `<a download>` + `a.click()` JavaScript per i download (vedi `docs/08-operations/troubleshooting.md`).

**woff2**
: Formato font web (Web Open Font Format 2). Non supportato da SkiaSharp: i file `.woff2` vengono rifiutati in upload. Convertire in `.ttf` o `.otf` (ADR-017).

---

## Y

**YGO** (Yu-Gi-Oh!)
: Abbreviazione usata nelle chiavi database e nei percorsi degli asset del gioco Yu-Gi-Oh!. Game key: `yugioh`.

