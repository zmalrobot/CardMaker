# 05 — Registro delle decisioni (ADR)

Formato: **contesto → decisione → conseguenze**. Le decisioni superate non si cancellano, si marcano.

---

## ADR-001 — Motore di rendering unico e data-driven
**Stato:** Accettata · 2026-09-01

**Contesto.** Il programma deve supportare Yu-Gi-Oh!, Pokémon e Magic, che hanno anatomie molto
diverse, e deve permettere all'admin di personalizzare *ogni dettaglio*.

**Decisione.** Un solo motore che interpreta un documento di layout dichiarativo. Giochi, tipi di
carta e template sono **dati nel database**, non classi C#.

**Conseguenze.**
- ✅ Aggiungere un gioco non richiede codice né deploy.
- ✅ La personalizzazione richiesta dal committente diventa possibile per costruzione.
- ⚠️ Il motore deve essere generale fin dall'inizio: costo maggiore su F1-F2.
- ⚠️ Serve un editor visuale (F7), altrimenti configurare i template è proibitivo.

---

## ADR-002 — SkiaSharp come libreria di rendering
**Stato:** Accettata · 2026-09-01

**Contesto.** Serve rendering 2D di alta qualità in C#, con controllo tipografico fine.

**Decisione.** SkiaSharp + SkiaSharp.HarfBuzz.

**Alternative scartate.** *ImageSharp*: tipografia meno controllabile, licenza commerciale sopra una
soglia di fatturato. *System.Drawing*: deprecato fuori da Windows. *Magick.NET*: pesante, orientato
al processing più che al disegno.

**Conseguenze.**
- ✅ `SKFont.ScaleX` risolve nativamente la compressione orizzontale del testo Yu-Gi-Oh!.
- ✅ Blend mode, maschere, path, gradienti: tutto disponibile.
- ✅ `SKDocument.CreatePdf` genera PDF con gli **stessi comandi di disegno** del raster.
- ⚠️ Dipendenza nativa: serve attenzione ai pacchetti runtime nel deploy.

---

## ADR-003 — Rendering server-side anche per l'anteprima
**Stato:** Accettata · 2026-09-01

**Contesto.** Il problema classico dei card maker è la divergenza fra anteprima (canvas HTML) e file
esportato (renderer server).

**Decisione.** L'anteprima è prodotta **dallo stesso motore**, a DPI ridotto, e restituita come PNG.

**Conseguenze.**
- ✅ Parità anteprima/export garantita per costruzione, non per disciplina.
- ✅ Un solo motore da mantenere e testare.
- ⚠️ Latenza di rete a ogni modifica → debounce, cancellazione delle richieste in volo, cache.
- ⚠️ L'editor WYSIWYG deve usare overlay DOM sopra l'immagine (vedi `02-architecture.md` §3.7).

---

## ADR-004 — Blazor con UI condivisa fra web e desktop
**Stato:** Accettata · 2026-09-01 · *aggiornata dall'[ADR-011](#adr-011--photinoblazor-come-host-desktop)*

**Contesto.** Servono entrambe le piattaforme, in C#, con risorse limitate.

**Decisione.** Tutta la UI in una Razor Class Library (`CardMaker.UI`), consumata da due host:
ASP.NET Core (Blazor Server interattivo) e una shell desktop basata su WebView.

**Conseguenze.**
- ✅ Una sola implementazione della UI.
- ✅ Blazor Server è adatto a ~10 utenti: nessun download WASM, accesso diretto a DB e renderer.
- ⚠️ I componenti non devono assumere l'esistenza di un server remoto: l'accesso ai dati passa da
  interfacce iniettate, implementate via HTTP nel web e in-process nel desktop.

---

## ADR-005 — Asset su filesystem content-addressed, non nel database
**Stato:** Accettata · 2026-09-01

**Decisione.** I binari sono salvati con nome = SHA-256 del contenuto; il database conserva solo i metadati.

**Conseguenze.**
- ✅ Deduplicazione automatica; backup e sincronizzazione semplici.
- ✅ **Nessun path traversal possibile**: il nome file non deriva mai dall'input utente.
- ✅ Cache HTTP immutabile.
- ⚠️ Serve un conteggio dei riferimenti per la cancellazione sicura degli asset orfani.

---

## ADR-006 — Nessuna valutazione di espressioni arbitrarie
**Stato:** Accettata · 2026-09-01

**Contesto.** Le regole condizionali dei template potrebbero essere espresse come stringhe da
interpretare o come script.

**Decisione.** Le condizioni sono un **AST tipizzato in JSON** con un insieme chiuso di operatori.
Nessun `eval`, nessuna compilazione a runtime, nessun motore di scripting.

**Conseguenze.**
- ✅ Elimina un'intera classe di vulnerabilità (code injection tramite template).
- ✅ Le regole sono rappresentabili in un builder visuale.
- ⚠️ Meno espressivo: se serviranno logiche complesse, si estenderà l'insieme degli operatori.

---

## ADR-007 — Versioni di template immutabili
**Stato:** Accettata · 2026-09-01

**Decisione.** Una `TemplateVersion` pubblicata non è più modificabile. Le carte salvate puntano a una
versione specifica. L'aggiornamento a una versione più recente è un'azione esplicita dell'utente.

**Conseguenze.**
- ✅ Una carta salvata non cambia mai aspetto senza consenso.
- ✅ Rende possibile un diff fra versioni e il rollback.
- ⚠️ Occorre gestire la crescita delle versioni (archiviazione).

---

## ADR-008 — Coordinate normalizzate 0..1
**Stato:** Accettata · 2026-09-01

**Decisione.** Le posizioni dei layer sono frazioni delle dimensioni della carta (trim), non pixel né mm.

**Conseguenze.**
- ✅ Lo stesso layout funziona identico a 96 DPI (anteprima) e a 600 DPI (export).
- ✅ Cambiare il DPI target non richiede di ridisegnare nulla.
- ⚠️ L'editor deve mostrare all'admin anche i valori in mm, più intuitivi.

---

## ADR-009 — Bypass admin locale confinato all'assembly desktop
**Stato:** Accettata · 2026-09-01

**Contesto.** Requisito: nella versione locale deve esistere un admin di default senza login.
Al tempo stesso la web app è **esposta su internet**, quindi un errore qui sarebbe grave.

**Decisione.** Il provider che concede il ruolo `Admin` esiste **solo** nel progetto
`CardMaker.Desktop`, dove l'app gira in-process con Photino e **non apre alcun listener di rete**.
`CardMaker.Web` esegue un controllo di startup che ne **fa fallire l'avvio** se rilevasse un provider
di bypass o `AppMode=Desktop` (fail-fast, mai fail-open).

**Conseguenze.**
- ✅ Comodità in locale senza aprire alcun buco nel deployment pubblico.
- ✅ Assenza di superficie di rete in modalità desktop: non c'è nulla da attaccare da remoto.
- ⚠️ Chiunque abbia accesso fisico alla macchina è admin dell'installazione locale: accettabile,
  documentato nel disclaimer, e comunque su un database separato da quello del server.

---

## ADR-015 — Infrastructure dipende da Rendering
**Stato:** Accettata · 2026-09-01 (in F0)

**Contesto.** Il generatore di segnaposto deve produrre immagini (SkiaSharp, quindi `Rendering`) e
registrarle come asset (quindi `Infrastructure`). Serviva decidere chi dipende da chi.

**Decisione.** `Infrastructure` fa riferimento a `Rendering`. Le porte (`IAssetCatalog`,
`IPlaceholderSeeder`, `IAssetStore`, `IImageProcessor`) vivono in `Application`, cosi' la UI non
vede mai `Infrastructure`.

**Conseguenze.**
- ✅ Nessun ciclo: `Rendering` resta una foglia che dipende solo da `Contracts`.
- ✅ Web e, in F8, Desktop condividono lo stesso seeder senza duplicare codice.
- ⚠️ SkiaSharp compare in due progetti (`Rendering` per il disegno, `Infrastructure` per la
  normalizzazione delle immagini in upload): accettabile, sono usi distinti.

---

## ADR-016 — Le misure derivano dall'invariante, non da arrotondamenti indipendenti
**Stato:** Accettata · 2026-09-01 (in F0)

**Contesto.** La prima stesura di `06-asset-spec.md` indicava un master canvas di 1488 × 2126 px,
ottenuto arrotondando separatamente 63 mm e 90 mm. Il primo test automatico ha mostrato che
86 mm a 600 DPI valgono **2031** px, quindi il master corretto e' **1488 × 2125**.

**Decisione.** L'unica fonte di verita' e' `CardGeometry`, che calcola
**master = trim + 2 × abbondanza**. Le misure pubblicate al grafico sono verificate da
`CardGeometryTests`: se cambiano, il test fallisce prima che vengano prodotti asset sbagliati.

**Conseguenze.**
- ✅ Documento e codice non possono piu' divergere in silenzio.
- ✅ Errore intercettato prima che qualcuno disegnasse 30 frame di misura sbagliata.

---

## ADR-017 — Un font per ruolo, non un font per carta
**Stato:** Accettata · 2026-09-01 (in F0)

**Contesto.** La prima stesura trattava i font come semplici asset. Ma ogni elemento di una carta ha
una tipografia propria: il nome, il testo effetto, i numeri ATK e il codice set sono font diversi.

**Decisione.** Ogni font caricato riceve un **alias di ruolo** (`card-name`, `effect-italic`,
`atk-def-value`…), univoco per gioco. I layout referenziano l'alias, non il file. La risoluzione
avviene al momento del render.

**Conseguenze.**
- ✅ Cambiare il font di tutti i nomi carta = ricaricare un file, senza toccare alcun template.
- ✅ Lo stesso file puo' coprire piu' ruoli: si assegnano piu' alias.
- ✅ Un alias non assegnato **non fa fallire il render**: si usa il fallback e si emette un avviso.
- ⚠️ I `.woff2` vengono rifiutati: SkiaSharp non li sa aprire, quindi sarebbero illeggibili in render.

---

## ADR-018 — Il modello C# è lo schema del layout
**Stato:** Accettata · 2026-09-01 (in F1)

**Contesto.** Il piano prevedeva la validazione del layout tramite JSON Schema.

**Decisione.** Lo schema è il modello tipizzato in `CardMaker.Contracts.Layout`, con polimorfismo
System.Text.Json sul discriminatore `type`. `LayoutSerializer.Validate` verifica solo i vincoli che
i tipi non esprimono (id duplicati, stili inesistenti, operatori sconosciuti).

**Conseguenze.**
- ✅ Una sola fonte di verita': niente schema da tenere allineato al codice.
- ✅ Errori di deserializzazione intercettati dal type system.
- ⚠️ Se in futuro servira' validare layout da strumenti esterni, il JSON Schema andra' **generato**
  dal modello, non scritto a mano.

---

## ADR-019 — L'altezza del testo si misura sul quadratone, non sui metrics del font
**Stato:** Accettata · 2026-09-01 (in F1)

**Contesto.** Usando `ascent + descent` come ingombro verticale, testi perfettamente plausibili
venivano dichiarati in overflow: quei metrics includono spazio per accenti e discendenti che quasi
nessuna stringa usa davvero.

**Decisione.** Il budget verticale è **`(righe − 1) × interlinea + corpo del font`**. L'interlinea
vale solo *fra* le righe. I metrics servono unicamente a posizionare la baseline, centrando il
glifo nel quadratone.

**Conseguenze.**
- ✅ Corrisponde al modello mentale di chi disegna un template: "testo da 12pt in una casella da 12pt".
- ✅ Su una riga sola l'interlinea non consuma piu' spazio.
- ⚠️ Ascendenti e discendenti possono sbordare di poco dalla casella: succede anche sulle carte vere,
  e resta governabile con `paddingYPt`.

---

## ADR-020 — Padding orizzontale e verticale separati
**Stato:** Accettata · 2026-09-01 (in F1)

**Contesto.** Con un unico valore di padding, le caselle sottili (nome, type line, ATK) perdevano
gran parte dell'altezza utile e finivano in overflow.

**Decisione.** `paddingXPt` e `paddingYPt` distinti.

**Conseguenze.**
- ✅ Le caselle di testo delle carte hanno margine laterale ma quasi nullo sopra e sotto, come serve.
- ⚠️ Chi scrive un template deve impostarne due invece di uno: l'editor di F7 mosterà entrambi.

---

## ADR-021 — I timestamp sono persistiti come tick UTC
**Stato:** Accettata · 2026-09-01 (in F1)

**Contesto.** SQLite non sa ordinare ne' confrontare i `DateTimeOffset`: qualsiasi
`ORDER BY CreatedAtUtc` falliva a runtime.

**Decisione.** Converter globale `DateTimeOffset ↔ long` (tick UTC), registrato nelle convenzioni
pre-modello del `DbContext`.

**Conseguenze.**
- ✅ Ordinamenti e confronti su data funzionano in SQL.
- ✅ Rappresentazione compatta e ordinabile.
- ⚠️ L'offset originale non viene conservato: irrilevante, tutti i timestamp sono generati in UTC.

---

## ADR-010 — Nessun asset grafico distribuito con l'applicazione
**Stato:** Accettata · 2026-09-01

**Decisione.** Frame, simboli, font e retro carta sono **esclusivamente** caricati dall'admin.
L'applicazione include solo placeholder generati proceduralmente e font di fallback liberi.
Ogni asset ha campi obbligatori di provenienza e licenza.

**Conseguenze.**
- ✅ Nessun materiale protetto viene ridistribuito con il software.
- ⚠️ L'app "vuota" non è utilizzabile: serve un pacchetto gioco per iniziare.
- ⚠️ La responsabilità sugli asset ricade sull'admin: va esplicitato nel disclaimer.
- ℹ️ Gli asset Yu-Gi-Oh! **non esistono ancora**: verranno realizzati su misura seguendo
  [`06-asset-spec.md`](06-asset-spec.md). Fino ad allora si sviluppa con placeholder procedurali
  costruiti sulle stesse misure, così la sostituzione sarà un semplice re-upload.

---

## ADR-011 — Photino.Blazor come host desktop
**Stato:** Accettata · 2026-09-01

**Contesto.** Il desktop deve girare su **Windows, macOS e Linux**.

**Decisione.** Usare **Photino.Blazor**.

**Alternative scartate.**
- *.NET MAUI Blazor Hybrid*: **non supporta Linux**. Escluso dal requisito.
- *Electron.NET*: bundle enorme, progetto in stallo.
- *Avalonia + WebView*: integrazione WebView ancora immatura.
- *Server locale + browser di sistema*: robusto e a zero dipendenze native, ma esperienza meno
  "applicativa". **Conservato come piano B** se WebKitGTK desse problemi su Linux.

**Conseguenze.**
- ✅ Un solo eseguibile per i tre OS, riusando la stessa Razor Class Library.
- ✅ Runtime leggerissimo (usa la WebView di sistema), licenza MIT.
- ⚠️ Dipende dalla WebView del sistema: **WebKitGTK su Linux** è il punto più fragile, va testato
  su Ubuntu LTS e documentato tra i prerequisiti.
- ⚠️ Ecosistema più piccolo di MAUI: meno esempi e componenti nativi (dialoghi file, notifiche).

---

## ADR-012 — La web app è pubblica: registrazione a invito e hardening completo
**Stato:** Accettata · 2026-09-01

**Contesto.** L'applicazione sarà raggiungibile da internet, pur avendo solo ~10 utenti reali.
Un'app pubblica riceve traffico automatizzato ostile a prescindere dal numero di utenti legittimi.
Il rendering a 600 DPI è CPU-intensivo, quindi è un bersaglio DoS naturale.

**Decisione.**
1. **Nessuna registrazione libera**: solo inviti monouso a scadenza generati dall'admin.
2. Hardening completo secondo la checklist di [`02-architecture.md` § 9.2](02-architecture.md).
3. Rate limiting e quote **per utente** su anteprima, upload ed export.
4. Contenuti caricati dagli utenti serviti da un **sottodominio separato senza cookie**.

**Conseguenze.**
- ✅ Superficie d'attacco ridotta al minimo compatibile col caso d'uso.
- ✅ Il costo di gestione resta basso: nessuna moderazione di utenti sconosciuti.
- ⚠️ Aggiunge la fase F9 alla roadmap: non è lavoro opzionale.
- ⚠️ L'admin deve invitare manualmente ogni nuovo utente.

---

## ADR-013 — Solo sRGB, nessuna preparazione per stampa professionale
**Stato:** Accettata · 2026-09-01

**Contesto.** Il committente ha escluso la stampa professionale.

**Decisione.** Pipeline interamente **sRGB**. Nessuna conversione CMYK, nessun profilo ICC, nessun
crocino di taglio, nessuna imposizione su foglio. L'abbondanza resta disponibile come opzione di
export (utile anche per il ritaglio casalingo) ma non è obbligatoria.

**Conseguenze.**
- ✅ Elimina l'intera complessità della gestione colore.
- ✅ Gli asset possono essere prodotti direttamente in sRGB (vedi `06-asset-spec.md`).
- ⚠️ Se in futuro servisse la stampa professionale, andrà aggiunta una conversione colore e i
  segni di taglio: modifica confinata alla fase POST della pipeline di rendering.

---

## ADR-014 — Rush Duel incluso nella v1
**Stato:** Accettata · 2026-09-01

**Contesto.** Richiesta esplicita del committente.

**Decisione.** Rush Duel entra nello scope della v1. Aggiunge ~9 template, il box effetto a **sezioni
etichettate** (`[REQUISITO]` / `[EFFETTO]`), le carte **LEGEND** (simbolo condizionale) e i
**Maximum Monster** (tre carte che compongono un'illustrazione panoramica).

**Decisione aperta:** se modellarlo come `CardType` aggiuntivi dello stesso `Game` oppure come
`Game` separato che riusa gli stessi asset. Da decidere in **F3**.

**Conseguenze.**
- ✅ **Non richiede nuovi tipi di layer**: è la prima verifica concreta che il design data-driven
  regge. Se dovesse richiederli, il design va rivisto subito e non a v2.
- ⚠️ Serve una nuova capacità nello slot immagine: il **crop "a fetta"** per i Maximum Monster
  (l'utente carica un'immagine panoramica e sceglie quale terzo mostrare).
- ⚠️ Aumenta il volume di asset da produrre (~30% in più).

---

## ADR-022 — `symbolRepeater` a griglia fissa e `toggleGroup` a posizioni relative
**Stato:** Accettata · 2026-09-01 (in F2)

**Contesto.** Il motore doveva coprire due esigenze grafiche di Yu-Gi-Oh!: le stelle di
Livello/Rank (un numero variabile di simboli identici, 1-12) e le frecce Link (8 posizioni fisse,
ciascuna on/off). Nessuna delle due si esprime bene con i layer esistenti (`symbolSlot` disegna un
solo simbolo).

**Decisione.**
- **`symbolRepeater`**: la griglia ha sempre `MaxCount` posizioni a passo fisso (non solo quelle
  riempite): il conteggio reale (da campo o letterale) decide quante, a partire da sinistra
  (`LeftToRight`, Rank) o da destra (`RightToLeft`, Livello). Questo evita che le stelle "si
  spostino" cambiando dimensione al variare del livello, come sulle carte vere.
- **`toggleGroup`**: le posizioni non sono calcolate ma **elencate esplicitamente** come
  `ToggleItem { Key, Rect }`, con `Rect` normalizzato **relativo al rettangolo del gruppo** (non
  alla carta). Ogni posizione si accende confrontando `Key` con gli elementi di una lista letta da
  campo. Questo stesso pattern (item con rettangolo relativo al contenitore) è deliberatamente
  riusabile per `repeatingBlock` in F11.

**Conseguenze.**
- ✅ Nessuna logica per-gioco nel motore: entrambi i layer restano generici, solo i dati del
  template (posizioni, direzione, chiavi) cambiano da un gioco all'altro.
- ✅ Un simbolo mancante produce un avviso (`symbol.missing`), mai un render silenzioso.
- ⚠️ `toggleGroup` richiede che chi scrive il template elenchi a mano le 8 posizioni delle frecce
  Link: non è calcolato, va copiato una volta nel template Link e poi riusato.

---

## ADR-023 — Chiusura F2: `richText`, `overlay`, crop a fetta, cache, golden test, PDF, HarfBuzz
**Stato:** Accettata · 2026-09-01 (in F2)

**Contesto.** Completamento della fase F2. Diverse decisioni indipendenti, raggruppate qui perché
tutte chiudono voci della stessa fase.

**`richText` senza auto-fit.** A differenza di `text`, `richText` disegna a **dimensione fissa** con
word-wrap semplice: niente `shrink`/`condense`. Motivazione: l'auto-fit su un layout con corsivo,
grassetto e simboli inline di larghezza diversa avrebbe richiesto rifare la fase MEASURE del motore
di testo per un formato misto, un lavoro che non ripaga finché non c'è un caso reale che lo richieda.
Se il testo non entra, si segnala `text.overflow` come per `text`. Il markup supportato è minimale
e non-arbitrario (coerente con ADR-006): `**grassetto**`, `*corsivo*`, `{sym:set.chiave}` per i
simboli inline, `[LABEL]` a inizio riga per le sezioni etichettate (usato per Rush Duel
`[REQUISITO]`/`[EFFETTO]`: **non serve un layer dedicato**, risolve la decisione rinviata #2), righe
`- testo` per i punti elenco.

**`overlay` con maschera via `SaveLayer`.** Il blend mode e l'opacità del layer si applicano quando
il contenuto isolato (`SaveLayer`) si fonde con lo sfondo, non mentre si disegna al suo interno:
così la maschera modella la trasparenza dell'overlay *prima* che il blend mode (es. `multiply` per
il foil) veda gli strati sottostanti. Nessuna capacità nuova nel senso di ADR-001: è composizione
Skia standard esposta come dato.

**Crop "a fetta" su `imageSlot`, non un layer dedicato.** `SliceCount`/`SliceIndex`/`SliceAxis` sono
proprietà aggiunte a `ImageSlotLayer` invece di un nuovo tipo di layer: i Maximum Monster Rush sono
comunque un'illustrazione con inquadratura, cambia solo *quale porzione* mostrare.

**Campi calcolati: nessun nuovo operatore.** `typeLine`, `linkRating`, `maximumAtkLine` erano
segnati come "campi calcolati da aggiungere", ma sono tutti esprimibili con `Join`/`Concat`/`Count`
già esistenti (es. `Source = "LINK-{{linkRating}}"` con binding diretto). Nessuna modifica al motore:
la voce si chiude in fase di scrittura dei template (F3), non nel motore.

**Cache LRU per SHA-256, non per byte[].** La cache degli asset decodificati vive in
`IDecodedImageCache` (singleton), chiave = `Asset.Sha256` (identità content-addressed già
esistente, ADR-005): evita di ridecodificare lo stesso frame/simbolo a ogni anteprima. Per non
disporre due volte la stessa `SKImage`, `PreloadedRenderResources` distingue immagini **possedute**
(decodificate ad-hoc, smaltite a fine richiesta) da immagini **in prestito** dalla cache condivisa
(mai smaltite dalla singola richiesta). Una seconda cache, quella dei *render* completi
(`CardRender` in F5, chiave layout+valori+dpi+formato), resta fuori da F2: appartiene al flusso di
salvataggio/esportazione delle carte, non al motore.

**Golden image test con font deterministico.** Possibili perché F0/F1 già garantivano un font di
test embedded (Roboto) invece del font di sistema: senza quel passaggio i golden test sarebbero
stati instabili fra Windows e Linux. Tolleranza per-canale (12/255) e frazione massima di pixel
diversi (1%), non uguaglianza esatta: assorbe il jitter dell'antialiasing.

**PDF riusa i PNG già renderizzati.** `PdfExporter` non ha un secondo percorso di disegno: incolla
in una pagina `SKDocument` l'immagine già prodotta da `CardRenderer`, dimensionata in punti da
pixel e DPI. Estende ADR-003 (anteprima ed export condividono lo stesso motore) anche al PDF.

**HarfBuzz: deliberatamente non implementato.** Serve per legature e scritture non latine; italiano,
inglese e le lingue finora in scope non ne hanno bisogno (nota già in F1). Implementarlo ora senza
un caso d'uso reale sarebbe complessità non ripagata. Resta in roadmap come voce futura, da
riprendere se un gioco richiederà scritture non latine.

**Conseguenze.**
- ✅ Tutte le voci di F2 sono chiuse con codice reale e test, tranne HarfBuzz (deferred, motivato).
- ✅ Nessuna delle decisioni sopra introduce logica per-gioco nel motore.
- ⚠️ `richText` non fa auto-fit: un template che ci mette troppo testo lo scopre da un avviso di
  overflow, non da un ridimensionamento automatico. Da tenere presente scrivendo i template F3.

---

## ADR-024 — Regole di selezione template riusano lo stesso AST condizionale del motore
**Stato:** Accettata · 2026-09-01 (in F3)

**Contesto.** Un `CardType` può avere template alternativi in base ai valori compilati dall'utente
(es. la fetta `left`/`center`/`right` di un Maximum Monster, o varianti grafiche specifiche). Serviva
un meccanismo per selezionare automaticamente il template appropriato.

**Decisione.** `Template.SelectionRuleJson` memorizza un `Condition` JSON interpretato da
`ConditionEvaluator` e `ValueBinder` già presenti nel motore di rendering. `ITemplateSelector` valuta
i template in ordine di `SortOrder` e restituisce il primo che soddisfa la condizione, con fallback sul
template `IsDefault` o il primo disponibile.

**Conseguenze.**
- ✅ Una sola grammatica condizionale in tutta l'applicazione (ADR-006): nessuna divergenza fra regole
  di visualizzazione layer e regole di selezione template.
- ✅ Zero logica ad-hoc o codice hardcodato: la selezione è 100% data-driven.

---

## ADR-025 — Formato pacchetto `.cmpkg` (zip) con protezione Zip-Slip
**Stato:** Accettata · 2026-09-01 (in F3)

**Contesto.** Per esportare e re-importare un gioco completo fra istanze diverse senza distribuire
asset grafici con il repository (ADR-010), serve un formato di interscambio autonomo.

**Decisione.** Il formato `.cmpkg` è un archivio standard ZIP contenente:
- `manifest.json` con `SchemaVersion`, `GameKey`, `ExportedAtUtc`.
- `game.json` con l'intero grafo relazionale (`Game`, `CardType`, `FieldDefinition`, `Template`,
  `TemplateVersion`, `SymbolSet`, `OptionList`, `Trait`, metadati `Asset` e `FontAsset`).
- Cartella `assets/{sha256}` contenente i file binari corrispondenti agli asset content-addressed.
Durante l'import, per prevenire attacchi di tipo **Zip-Slip**, le voci sono accettate solo se iniziano
con `assets/`, non contengono `..` e non sono percorsi assoluti.

**Conseguenze.**
- ✅ Backup, migrazione e condivisione di giochi e template completamente portabili e autoconsistenti.
- ✅ Protezione rigorosa del filesystem contro path traversal da pacchetti malevoli.
- ⚠️ Se un pacchetto esportato fa riferimento ad asset non presenti fisicamente nello store, lo schema
  viene comunque importato ma il binario viene segnalato/scartato in sicurezza.

---

## ADR-026 — Rush Duel modellato come CardTypes aggiuntivi nello stesso Game Yu-Gi-Oh!
**Stato:** Accettata · 2026-09-01 (in F3)

**Contesto.** Decisione rinviata #1: decidere se Rush Duel debba essere un `Game` a sé stante o una serie
di `CardType` all'interno del `Game` Yu-Gi-Oh! classico.

**Decisione.** Rush Duel condivide la stessa geometria (59 × 86 mm, trim, safe zone, DPI), le stesse
regole di fondo, gli stessi attributi (`dark`, `light`...), le stesse razze e gli stessi set di simboli
di base di Yu-Gi-Oh!. Pertanto è modellato come insieme di `CardType` (`rush-monster-*`, `rush-spell`,
`rush-skill`, `rush-monster-maximum`) all'interno del medesimo `Game` `yugioh`.

**Conseguenze.**
- ✅ Riuso immediato di font, attributi, razze e rarità senza duplicare cataloghi.
- ✅ Nessuna frammentazione nel database per giochi della stessa famiglia con identica geometria.
---

## ADR-027 — Design system su Bootstrap 5.3, token CSS e persistenza tema ibrida (Cookie + LocalStorage)
**Stato:** Accettata · 2026-09-01 (in F4)

**Contesto.** L'interfaccia deve supportare tema chiaro e tema scuro su palette blu/azzurro, funzionare
sia su Blazor Server (con prerender SSR) sia su desktop (Photino), ed evitare il "flash" di colore non
stilizzato (FOUC). Il fondo dell'anteprima delle carte non deve mai essere blu per non falsare la
percezione cromatica dell'artwork e del frame.

**Decisione.**
- Base su **Bootstrap 5.3** con `data-bs-theme="light|dark"` e mapping sui token proprietari `--cm-*`.
- **Fondo anteprima carta neutrale**: trama a scacchiera (`--cm-checkerboard-1/2`) o grigio neutro
  indipendente dal tema dell'app.
- **Persistenza ibrida del tema**: `localStorage` (client) + cookie `cm-theme` (server) + script inline
  sincrono nell'`<head>` di `App.razor` prima dell'avvio di Blazor.

**Conseguenze.**
- ✅ Zero FOUC al caricamento della pagina e pieno supporto SSR.
- ✅ Contrasto WCAG AA garantito su entrambi i temi.
- ✅ Componenti e stili centralizzati in `CardMaker.UI`, riutilizzabili identici in Web e Desktop.

---

## ADR-028 — Ciclo di vita carte utente, form dinamico, anteprima debouncata ed export multiformato
**Stato:** Accettata · 2026-09-01 (in F5)

**Contesto.** Gli utenti devono poter creare, modificare, duplicare, eliminare ed esportare carte in modo
intuitivo e fluido. Il form deve adattarsi dinamicamente ai campi del `CardType` (`FieldDefinition`) con
valutazione delle condizioni `VisibleWhen`, e l'anteprima di rendering deve aggiornarsi in tempo reale
senza sovraccaricare il server durante la digitazione.

**Decisione.**
- **Wizard a 2 step**: selezione gioco/tipo carta/tratti → editor avanzato (`TwoColumnLayout`).
- **Form dinamico condizionale**: `DynamicCardForm` genera i controlli per ciascun `FieldKind` e valuta
  `VisibleWhen` tramite `ConditionEvaluator` e `ValueBinder`.
- **Live preview debouncata**: ritardo di 200 ms con cancellazione cooperativa (`CancellationTokenSource`)
  delle richieste di render intermedie; risoluzione automatica del template tramite `ITemplateSelector`.
- **Export multiformato**: `CardExportService` produce file PNG, JPEG o PDF a 600 DPI (con supporto PDF
  multipagina fronte/retro e opzione bleed).

**Conseguenze.**
- ✅ Esperienza utente reattiva ed ergonomica sia su desktop che mobile.
- ✅ Isolamento multi-utente tramite `OwnerUserId` e persistenza serializzata dei valori in JSON.
- ✅ Nessun secondo percorso di rendering: l'export riusa la stessa pipeline SkiaSharp del motore.

---

## ADR-029 — Gestione Contenuti Admin, Schema Editor con Anteprima Live, Operazioni Asset Sicure e Audit Log
**Stato:** Accettata · 2026-09-01 (in F6)

**Contesto.** L'amministratore deve poter configurare nuovi giochi, tipi di carta, tratti, simboli, opzioni
e campi senza mai toccare il codice sorgente o il database. Inoltre, la cancellazione accidentale di asset
grafici utilizzati in template o simboli deve essere impedita, e le modifiche ai file devono preservare
la stabilità degli identificatori univoci tracciando ogni operazione per sicurezza e auditability.

**Decisione.**
- **Hub di Gestione Contenuti (`/admin/content`)**: navigazione a schede reattiva per Giochi, Tipi di carta,
  Tratti, Simboli e Liste opzioni.
- **Editor Schema Campi con Live Form Preview (`/admin/schema/{id}`)**: interfaccia a due colonne che
  affianca l'editing delle `FieldDefinition` (riordinamento, `VisibleWhen`, validazioni) con l'anteprima
  interattiva in tempo reale (`DynamicCardForm`).
- **Operazioni Asset Sicure**:
  - `CheckAssetUsageAsync` blocca la cancellazione di asset referenziati in simboli, font, card thumbnail o layout.
  - `ReplaceAssetBlobAsync` aggiorna il blob storage e l'impronta SHA-256 mantenendo invariato l'ID `Guid`.
  - Caricamento batch multiplo (`InputFile multiple`).
- **Registro Audit (`AuditLogEntry`)**: tracciamento automatico di tutte le azioni amministrative con timestamp UTC,
  utente, tipo entità, ID e payload JSON delle modifiche.

**Conseguenze.**
- ✅ Nuovi giochi (es. Pokémon TCG o Magic) e tipi di carta sono configurabili al 100% da interfaccia.
- ✅ Zero rischio di "broken image" nei template o nelle carte esistenti.
- ✅ Tracciabilità completa delle modifiche amministrative.

---

## ADR-030 — Studio Template WYSIWYG a 3 Pannelli, Validazione Layout Statica e Versioning Immutabile
**Stato:** Accettata · 2026-09-01 (in F7)

**Contesto.** La creazione e modifica dei template grafici (`CardLayout`) necessita di uno studio WYSIWYG
interattivo che permetta all'amministratore di comporre layer (cornici, artwork, testi, simboli, ripetitori,
frecce link, foil) senza manipolare JSON a mano, con anteprima server-side fedele al motore SkiaSharp,
guide di stampa (safe zone, bleed), validazione statica e gestione delle versioni per garantire l'immutabilità
delle carte esistenti (ADR-007).

**Decisione.**
- **Studio a 3 Pannelli (`/admin/templates/{id}`)**:
  - *Pannello Sinistro (Layer Tree)*: albero gerarchico dei layer con riordinamento z-order, aggiunta rapida
    dei vari tipi di layer polimorfi (`StaticImage`, `ImageSlot`, `Text`, `RichText`, `SymbolSlot`, `SymbolRepeater`,
    `ToggleGroup`, `Shape`, `Overlay`), duplicazione e rimozione.
  - *Pannello Centrale (Canvas Viewport)*: anteprima server-side SkiaSharp debouncata (200 ms), zoom scalabile (50% - 150%),
    guide perimetrali Safe Zone (3 mm), Bleed (2 mm), griglia di allineamento e bounding box evidenziatore del layer attivo.
  - *Pannello Destro (Layer Inspector)*: configuratore delle proprietà geometriche (coordinate normalizzate 0..1 `NormalizedRect`)
    e delle proprietà per-tipo (font, slot artwork, simboli) + **Visual Condition Builder** per comporre regole `VisibleWhen` senza digitare JSON.
- **Validatore Statico del Layout (`ValidateLayoutAsync`)**:
  - Segnala errori per dimensioni nulle o negative (`layer.zero_size`).
  - Segnala avvisi per layer fuori dai bordi normalizzati (`layer.out_of_bounds`).
  - Rileva token o binding a campi non definiti nello schema del tipo di carta (`text.binding_unmapped`, `slot.key_unmapped`).
- **Gestione Versioni (`TemplateVersion`)**:
  - Ogni salvataggio genera una nuova versione incrementale (bozza o pubblicata).
  - La pubblicazione di una versione rende attivi i cambiamenti preservando le carte create con versioni precedenti.

**Conseguenze.**
- ✅ Creazione visuale completa di template senza toccare JSON.
- ✅ Rilevamento preventivo di errori di layout e di binding orfani.
- ✅ Salvaguardia dell'immutabilità e retrocompatibilità del catalogo carte.

---

## ADR-031 — Host Desktop Cross-Platform Photino.Blazor, Directory di Sistema OS e Bypass Admin Locale Offline
**Stato:** Accettata · 2026-09-01 (in F8)

**Contesto.** L'applicazione deve poter essere eseguita come programma desktop nativo su Windows, macOS e Linux,
funzionando al 100% offline senza richiedere un server web attivo, conservando il database SQLite e gli asset
grafici nelle cartelle standard del sistema operativo, e fornendo un accesso amministratore automatico in-process
senza aprire falle di sicurezza sull'host web pubblico (ADR-009, ADR-011).

**Decisione.**
- **Host Desktop con Photino.Blazor (`CardMaker.Desktop`)**:
  - Utilizza la WebView nativa del sistema operativo (WebView2 su Windows, WKWebView su macOS, WebKitGTK su Linux).
  - Riuso al 100% dei componenti Blazor di `CardMaker.UI` e dei servizi di rendering in-process.
- **Risoluzione Percorsi di Sistema (`DesktopPathResolver`)**:
  - **Windows**: `%LOCALAPPDATA%\CardMaker\`
  - **macOS**: `~/Library/Application Support/CardMaker/`
  - **Linux**: `$XDG_DATA_HOME/CardMaker/` o `~/.local/share/CardMaker/`
  - Creazione automatica delle sottocartelle `assets/`, `fonts/` e del DB `CardMaker.db`.
- **Bypass Admin Locale (`DesktopAuthenticationStateProvider`)**:
  - Identità predefinita con ruolo `Admin` e `ClaimTypes.NameIdentifier = "desktop-local-admin"`.
  - Confinato esclusivamente all'assembly `CardMaker.Desktop`, senza listener HTTP/TCP aperti.
- **Inizializzazione e Seeding Offline**:
  - All'avvio desktop, se il database è vuoto, viene eseguita la creazione schema con migrazioni e il seeding
    automatico dei segnaposto e del pacchetto di carte Yu-Gi-Oh! / Rush Duel.

**Conseguenze.**
- ✅ App desktop stand-alone funzionante offline su Windows, macOS e Linux.
- ✅ Nessuna duplicazione di logica di rendering o di UI tra Web e Desktop.
- ✅ Isolamento sicuro delle credenziali tra ambiente desktop e ambiente server pubblico.

---

## ADR-032 — Hardening Web, Registrazione Rigorosamente a Invito, Security Headers e Snapshot SQLite Online
**Stato:** Accettata · 2026-09-01 (in F9)

**Contesto.** La web app CardMaker viene distribuita pubblicamente su internet. Anche con ~10 utenti legittimi,
l'applicazione riceve traffico automatizzato ostile. Il rendering a 600 DPI è una risorsa costosa (bersaglio DoS naturale)
e la registrazione aperta costituirebbe un vettore di abuso immediato (ADR-012).

**Decisione.**
- **Registrazione Esclusivamente su Invito (`IInvitationService`)**:
  - Nessun self-signup pubblico: la registrazione richiede un token univoco crittografico valido a scadenza (`/Account/Register?token=...`).
  - L'amministratore gestisce gli inviti da `/admin/invitations`.
  - I token vengono memorizzati sotto hash SHA-256 nel database e consumati atomicamente alla creazione dell'utente.
- **Security Headers & CSP Middleware (`SecurityHeadersMiddleware`)**:
  - Imposizione di `Content-Security-Policy` (compatibile con Blazor Server e `wasm-unsafe-eval`), `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, e HSTS in produzione.
- **Protezione Anti-DoS & Rate Limiting**:
  - Rate limiting sliding window su tutte le chiamate esterne con codice 429.
- **Snapshot Online e Verifica Integrità SQLite (`BackupService`)**:
  - Utilizzo del comando `VACUUM INTO` per generare copie transazionalmente coerenti del database a runtime senza lock bloccanti.
  - Verifica automatica dell'integrità tramite `PRAGMA integrity_check;` e dashboard admin `/admin/backups`.
- **Health Checks & Containerizzazione**:
  - Endpoint `/healthz` con probe sul DB.
  - `Dockerfile` multi-stage con utente non-root, librerie native SkiaSharp (`libfontconfig1`, `libfreetype6`) e stack `docker-compose` con Caddy reverse proxy e TLS automatico.
- **Fail-Fast Check (ADR-009)**:
  - `CardMaker.Web` rifiuta l'avvio se `AppMode=Desktop`.

**Conseguenze.**
- ✅ Superficie di attacco ridotta al minimo e protezione DoS/brute force sul rendering.
- ✅ Zero rischio di utenti non autorizzati.
- ✅ Backup atomici sicuri in WAL mode con verifica integrità in un click.

---

## ADR-034 — Estensione Multi-Gioco: Pokémon TCG & Magic: The Gathering
**Stato:** Accettata · 2026-09-03 (in F11 e F12)

**Contesto.** Per completare i tre giochi target previsti dalle specifiche iniziali, il sistema doveva includere il supporto completo
a Pokémon TCG e Magic: The Gathering (MTG), comprendendo modelli dati, layout demo, simboli procedurali, ruoli font dedicati con font
incorporati e isolamento dei filtri di gioco nella UI admin.

**Decisione.**
- **Seeding di Dominio Modulare**:
  - Creati `PokemonContentSeeder`, `PokemonFontSeeder`, `MtgContentSeeder`, `MtgFontSeeder` con relative interfacce in `CardMaker.Application`
    e cablati nel `DatabaseInitializer`.
- **Font Incorporati & Risoluzione**:
  - Incorporati come risorse TTF/OTF in `CardMaker.Infrastructure`: GillSansBold, GillSansItalic, GillSans, Futura-Bold per Pokémon;
    Beleren2016-Bold, Beleren2016SmallCaps-Bold, Mplantin per MTG.
  - `FontService` mappa automaticamente gli alias dei ruoli di tutti e tre i giochi garantendo fallback eleganti.
- **Simboli Procedurali SkiaSharp**:
  - `PlaceholderSymbolGenerator` esteso per generare su richiesta i simboli di mana MTG (`mtg-mana`: w, u, b, r, g, c, numeri 0-9, x, tap),
    i simboli di rarità MTG (`mtg-rarity`: common, uncommon, rare, mythic) e i simboli energia Pokémon (`pokemon-energy`: grass, fire, water, lightning, psychic, fighting, darkness, metal, colorless).
- **Filtri di Gioco per Risorse Admin**:
  - Libreria Asset, Ruoli Font, Segnaposto e Prova Motore filtrano rigorosamente le risorse e le opzioni in base al gioco selezionato.

**Conseguenze.**
- ✅ Supporto nativo completo per Yu-Gi-Oh!, Pokémon TCG e Magic: The Gathering.
- ✅ Autonomia completa anche offline o senza asset caricati grazie a font incorporati e simboli procedurali.

---

## ADR-035 — Centraggio Ottico CapHeight e Mappatura 1:1 dei Frame Master a Piena Abbondanza
**Stato:** Accettata · 2026-09-03

**Contesto.** Nei frame delle carte reali e segnaposto, il testo (es. nome della carta) deve apparire otticamente centrato all'interno
delle caselle grafiche, senza sbavature, sovrapposizioni alle linee di contorno o disallineamenti dovuti all'abbondanza (bleed).
In precedenza:
1. Il calcolo della baseline in `DrawFittedText` basato sull'altezza totale del bounding box (`Ascent` + `Descent`) spingeva verso l'alto
   i font con ascendenti marcate (es. Beleren per MTG), toccando la cornice superiore.
2. I frame generati a dimensione master (`MasterWidthPx` x `MasterHeightPx`) venivano disegnati all'interno del rettangolo `(0, 0, 1, 1)`
   (ovvero il trim), comprimendo l'abbondanza nel trim e causando uno sfasamento di ~12px rispetto ai livelli di testo.

**Decisione.**
- **Centraggio Ottico Verticale**:
  - `CardRenderer` calcola la baseline del testo a riga singola e multipla in base a `CapHeight` (`metrics.CapHeight > 0 ? metrics.CapHeight : -metrics.Ascent * 0.7f`).
  - La baseline per `VerticalAlign.Middle` è centrata esattamente su `box.MidY + (capHeight / 2f)`, allineando visivamente i caratteri maiuscoli
    con spaziatura perfettamente simmetrica sopra e sotto.
- **Mappatura Automatica Frame Master (Full-Bleed)**:
  - In `PaintStaticImage`, se un'immagine ha le dimensioni esatte del canvas master (`MasterWidthPx` e `MasterHeightPx`) e il layer copre
    l'intera carta (`NormalizedRect(0, 0, 1, 1)`), viene disegnata direttamente sul master canvas `(0, 0, MasterWidthPx, MasterHeightPx)`.
  - Questo elimina l'effetto di doppia applicazione del bleed e garantisce un allineamento pixel-perfect (0 pixel di scarto) tra frame e livelli.

**Conseguenze.**
- ✅ Carte composte con segnaposto e asset grafici allineate al millimetro su tutti i giochi.
- ✅ Resa tipografica professionale identica a quella degli strumenti di desktop publishing.

---

## ADR-036 — Disattivazione Verbosity IPC Photino e Logging Strutturato Sintetico
**Stato:** Accettata · 2026-09-04

**Contesto.**
Nell'host Desktop basato su Photino.Blazor (WebKitGTK su Linux), il livello di log verbosity predefinito stampava sul canale `stdout`
ogni messaggio IPC scambiato tra il processo .NET e la webview. Poiché i componenti Blazor inviano la bitmap dell'anteprima
sotto forma di stringa Base64 data-URI (`data:image/png;base64,...`), ogni aggiornamento di render riversava sulla console
centinaia di kilobyte di caratteri grezzi, bloccando l'I/O del terminale, rallentando l'esperienza utente e rendendo illeggibili i log.

**Decisione.**
- In `CardMaker.Desktop/Program.cs`, configurare `app.MainWindow.SetLogVerbosity(0)` per disattivare il dump IPC nativo di Photino.
- Integrare l'astrazione `ILogger<T>` standard di Microsoft.Extensions.Logging nei servizi di dominio e rendering:
  - `CardPreviewService`: log sintetico con ID, nome carta, DPI, dimensioni in pixel, peso in KB e tempo di rendering in ms (`[Preview]`).
  - `CardExportService`: log di export con nome file, formato, DPI e peso (`[Export]`).
  - `CardService`: log di ciclo di vita delle carte (`[Card] Creata / Aggiornata / Duplicata / Eliminata`).
  - `AssetService`: log di memorizzazione asset (`[Asset] Caricato asset ... px/bytes`).
- Nessun log deve mai stampare stringhe o payload Base64.

**Conseguenze.**
- ✅ Terminale pulito, leggibile e privo di latenze legate al buffer stdout.
- ✅ Diagnostica e tracciabilità preservate con log strutturati essenziali.

---

## ADR-037 — Ottimizzazione Asincrona UI 60 FPS e Hardware Acceleration per Blazor Desktop/Web
**Stato:** Accettata · 2026-09-04

**Contesto.**
All'interno dell'applicazione Desktop (Linux WebKitGTK) e Web, le operazioni di rendering della carta mostravano un'esperienza visiva
a tratti scattosa:
1. L'overlay di caricamento utilizzava `backdrop-filter: blur(6px)`, costringendo il rasterizzatore WebKitGTK a continui ricalcoli
   della sfocatura su tutto lo schermo durante la rotazione dell'animazione dello spinner CSS, facendo crollare il frame rate a 10-15 FPS.
2. Le chiamate a `CardPreviewService` e `CardExportService` eseguivano il recupero SQLite e la decodifica iniziale delle immagini Skia
   sul thread UI prima del context switch, bloccando il message loop.
3. La navigazione verso `/cards/create` evidenziava contemporaneamente sia "Le mie carte" che "Nuova carta" per via del matching di prefisso.

**Decisione.**
- **CSS Performance**: Rimosso `backdrop-filter: blur(6px)` in favore di un background scuro ad alto contrasto `rgba(13, 17, 23, 0.72)`
  e applicata accelerazione GPU con `transform: translateZ(0)` e `will-change: transform`.
- **Offload Skia Completo**: Tutta la catena di esecuzione (query EF Core per asset e font, decodifica Skia, rasterizzazione SkiaSharp
  e codifica PNG/PDF) è incapsulata in `Task.Run(...)`, liberando al 100% il thread UI Blazor.
- **Routing NavMenu**: Aggiunto `Match="NavLinkMatch.All"` su `href="cards"` in `DesktopNavMenu.razor` e `NavMenu.razor`.

**Conseguenze.**
- ✅ Animazioni di caricamento a 60 FPS senza micro-freeze o scatti.
- ✅ Esperienza utente fluida e perfettamente allineata tra desktop e web.
- ✅ Menu di navigazione coerente senza selezioni multiple errate.










