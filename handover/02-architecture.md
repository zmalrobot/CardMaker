# 02 — Architettura tecnica

## 1. Stack

| Ambito | Scelta | Motivo |
|---|---|---|
| Runtime | **.NET 10** (LTS, C# 14) — SDK 10.0.400 verificato sulla macchina | Richiesta committente: C# |
| UI | **Blazor** (componenti in una Razor Class Library condivisa) | Una sola UI per web e desktop |
| Host web | **ASP.NET Core** — Blazor Web App, render mode `InteractiveServer` | Con ~10 utenti il server-side è ideale: zero download WASM, accesso diretto a DB e renderer |
| Host desktop | **Photino.Blazor** | Unica opzione Blazor realmente **cross-platform Windows + macOS + Linux**. Usa la WebView di sistema (WebView2 / WKWebView / WebKitGTK), è leggera (~1 MB) e MIT. **.NET MAUI è escluso: non supporta Linux** |
| Database | **SQLite** + **EF Core 10** | Richiesta committente; file singolo, perfetto per desktop e per 10 utenti |
| Rendering 2D | **SkiaSharp** (+ `SkiaSharp.HarfBuzz` per lo shaping del testo) | Motore di Chrome/Flutter. Supporta font caricati da byte, `ScaleX` per la compressione orizzontale, blend mode per i foil, maschere, **e l'output PDF vettoriale** |
| Export PDF | **`SKDocument.CreatePdf`** di SkiaSharp | Stessi comandi di disegno del raster → parità garantita, senza una seconda libreria |
| Auth | **ASP.NET Core Identity** con ruoli | Standard, integrato con EF Core |
| Localizzazione UI | `.resx` + `IStringLocalizer` (it, en) | Standard .NET |
| Validazione JSON layout | **JSON Schema** (`JsonSchema.Net`) | Il layout è dato: va validato prima di essere eseguito |
| Test grafici | xUnit + **golden image testing** (diff pixel con tolleranza) | Unico modo serio di testare un renderer |

### Perché SkiaSharp e non ImageSharp

- SkiaSharp gestisce nativamente `ScaleX` sul font (**indispensabile per Yu-Gi-Oh!**), blend mode
  avanzati, maschere e path complessi.
- SkiaSharp genera **PDF vettoriale** con la stessa API di disegno del raster: un solo codice per
  PNG, JPG e PDF.
- ImageSharp ha una licenza commerciale sopra una certa soglia di fatturato; Skia è BSD.

---

## 2. Struttura della solution

```
CardMaker.sln
├─ src/
│  ├─ CardMaker.Domain           # Entità, value object, enum. Nessuna dipendenza.
│  ├─ CardMaker.Contracts        # Modelli del layout JSON + DTO condivisi + JSON Schema
│  ├─ CardMaker.Rendering        # ★ Motore SkiaSharp: layer, layout, testo, export
│  ├─ CardMaker.Application      # Servizi, casi d'uso, validazione, interfacce (porte)
│  ├─ CardMaker.Infrastructure   # EF Core/SQLite, Identity, AssetStore su filesystem, pacchetti
│  ├─ CardMaker.UI               # Razor Class Library: TUTTE le pagine e i componenti Blazor
│  ├─ CardMaker.Web              # Host ASP.NET Core (API + Blazor Server) — esposto su internet
│  └─ CardMaker.Desktop          # Shell Photino.Blazor (Win/macOS/Linux) + admin locale
├─ tests/
│  ├─ CardMaker.Rendering.Tests  # golden images
│  ├─ CardMaker.Application.Tests
│  └─ CardMaker.Web.Tests
├─ packages/                     # pacchetti gioco esportati (.cmpkg)
├─ handover/                     # questa cartella
└─ docs/
```

**Regola di dipendenza:** `Domain ← Application ← Infrastructure/Rendering ← Host`.
`CardMaker.Rendering` **non conosce il database**: riceve un `CardRenderRequest` già risolto e un
`IAssetResolver`. Questo lo rende testabile in isolamento e riutilizzabile.

---

## 3. Il motore di rendering (cuore del progetto)

### 3.1 Pipeline

```
1. RESOLVE   → scegli il TemplateVersion (regole di selezione) e carica il layout JSON
2. BIND      → sostituisci i binding {{campo}} con i valori della carta; calcola i campi derivati
3. EVALUATE  → valuta le condizioni visibleWhen; pota i layer non visibili
4. MEASURE   → auto-fit del testo, misura i blocchi ad altezza variabile, risolvi il flusso verticale
5. PAINT     → disegna su SKSurface (raster) o SKDocument (PDF), in ordine di z-index
6. POST      → maschera angoli arrotondati, gestione abbondanza, overlay foil, encoding finale
```

Le fasi 1-4 sono **indipendenti dal formato di output**: raster e PDF condividono tutto tranne la 5-6.

### 3.2 Tipi di layer

| Tipo | Descrizione |
|---|---|
| `staticImage` | Asset fisso: frame, bordo, ologramma |
| `imageSlot` | Immagine caricata dall'utente, con crop/zoom/pan e maschera opzionale |
| `text` | Testo su riga singola o multipla, con auto-fit |
| `richText` | Testo con run stilizzati (corsivo/grassetto), **simboli inline**, bullet, paragrafi |
| `symbolSlot` | Un simbolo scelto da un set (attributo, proprietà magia) |
| `symbolRepeater` | N ripetizioni di un simbolo (stelle livello, costi energia), direzione LTR/RTL |
| `toggleGroup` | Insieme di posizioni on/off (le 8 frecce Link) |
| `repeatingBlock` | Blocco ripetuto ad altezza variabile (attacchi Pokémon, abilità Planeswalker) |
| `shape` | Rettangolo/ellisse/path con fill, gradiente, bordo |
| `group` | Contenitore per traslare/mostrare più layer insieme |
| `overlay` | Texture foil applicata con blend mode e maschera |

Proprietà comuni a tutti: `id`, `name`, `z`, `rect` (coordinate **normalizzate 0..1** rispetto al
formato carta), `anchor`, `rotation`, `opacity`, `blendMode`, `visibleWhen`, `locked`.

> **Coordinate normalizzate**: rendono il layout indipendente dal DPI. Anteprima a 96 DPI ed export a
> 600 DPI usano lo stesso identico layout, moltiplicato per un fattore di scala.

### 3.3 Motore di testo — il punto più delicato

Requisiti:
- **Auto-fit** con strategia configurabile: `none`, `shrink`, `condense`, `shrinkAndCondense`,
  con `minFontSize` e `minScaleX` come limiti.
- **Compressione orizzontale** (`SKFont.ScaleX < 1`) — indispensabile per Yu-Gi-Oh!.
- **Interlinea** riducibile insieme al corpo font.
- **Word wrap** greedy con misurazione reale, ricerca binaria sulla dimensione ottimale.
- **Run inline**: il rich text è parsato in una lista di run `Text(stile)` / `Symbol(chiave)` /
  `LineBreak`, i simboli sono allineati alla baseline e scalati sull'altezza x del font.
- **Shaping** via HarfBuzz per legature, kerning e supporto a lingue non latine.

**Mini-linguaggio del rich text** (volutamente minimale, parsato con parser custom — **niente
esecuzione di codice**):

```
Testo normale
*corsivo*  **grassetto**
{sym:attribute.dark}      → simbolo inline
{{atk}}                   → binding a un campo
● bullet
\n                        → a capo forzato
```

### 3.3.1 Font: risoluzione per alias di ruolo

Ogni elemento testuale ha un **font proprio**, caricato dall'admin e identificato da un **alias di
ruolo** (`card-name`, `effect`, `effect-italic`, `atk-def-value`, `set-code`… — elenco completo in
[`06-asset-spec.md` § 9](06-asset-spec.md)).

```
layout: "font": "card-name"
           ↓
IFontProvider.Resolve("card-name")
           ↓
FontAsset del gioco con Alias = "card-name"  →  SKTypeface (in cache)
           ↓ se manca
font di fallback libero + segnalazione nel risultato del render
```

- Un file font puo' essere registrato con **piu' alias** se copre piu' ruoli.
- I `SKTypeface` sono caricati una sola volta e tenuti in cache per chiave asset.
- Un alias mancante **non fa fallire il render**: si usa il fallback e l'anomalia viene riportata,
  cosi' l'anteprima puo' avvisare l'admin invece di mostrare una pagina di errore.
- I font vengono **incorporati nel PDF** esportato, quindi la licenza deve consentirlo.

### 3.4 Espressioni e condizioni: nessun `eval`

Le condizioni sono un **AST in JSON**, non stringhe da interpretare:

```json
{ "op": "and", "args": [
    { "op": "eq",  "field": "rarity", "value": "ultra" },
    { "op": "gte", "field": "level",  "value": 5 },
    { "op": "in",  "field": "abilities", "value": ["tuner", "spirit"] }
]}
```

Operatori: `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `in`, `notIn`, `isEmpty`, `notEmpty`, `and`, `or`, `not`.

Motivo: valutare stringhe arbitrarie (o compilare C#/JS a runtime) sarebbe una **superficie di
attacco per code injection** ed è del tutto evitabile.

### 3.5 Campi calcolati

Alcuni valori non sono inseriti dall'utente ma derivati (es. la type line YGO
`[Dragon/Synchro/Tuner/Effect]`, o `LINK-4` dal numero di frecce attive).
Si modellano come **template di stringa dichiarativi** con join e filtri:

```json
{ "key": "typeLine", "expr": { "op": "join", "sep": "/", "prefix": "[", "suffix": "]",
  "args": ["{{race}}", "{{summonMethod}}", "{{abilities}}", "{{effectFlag}}"] } }
```

### 3.6 Parità anteprima ↔ export

Il rendering è **server-side**, quindi l'anteprima è **prodotta dallo stesso motore** a DPI ridotto
(~96–150 DPI, lato lungo ≈ 500 px) e restituita come PNG.

- Debounce lato client 200 ms, annullamento della richiesta in volo.
- Cache dei render con chiave `SHA256(layoutVersion + valori + asset + dpi + formato)`.
- Rate limiting sull'endpoint di anteprima.

Questo **elimina per costruzione** il classico problema "l'anteprima non corrisponde al file esportato".

### 3.7 Editor WYSIWYG con rendering server-side

Tecnica: l'editor mostra l'**immagine renderizzata dal server** come sfondo e vi sovrappone dei
**riquadri DOM trasparenti**, uno per layer, trascinabili e ridimensionabili.

```
┌─────────────────────────┐
│  <img>  anteprima PNG   │  ← generata dal motore reale
│  ┌────┐                 │
│  │drag│ ← <div> overlay │  ← handle di trascinamento/resize
│  └────┘                 │
└─────────────────────────┘
```

Durante il trascinamento si aggiorna solo l'overlay (60 fps, nessuna chiamata al server); al rilascio
si invia il nuovo `rect` e si richiede un nuovo render. Pannello proprietà a destra per tutto il resto.

---

## 4. Persistenza

### 4.1 Database

SQLite via EF Core, con migrazioni applicate all'avvio.
- **Web**: un file `cardmaker.db` in una cartella dati configurabile.
- **Desktop**: `%LOCALAPPDATA%\CardMaker\cardmaker.db`.

### 4.2 Asset store

Gli asset binari **non stanno nel database**: filesystem **content-addressed**.

```
assets/
  ab/cd/abcdef0123…​.png     ← nome file = SHA-256 del contenuto
```

Vantaggi: deduplicazione automatica, nomi file non manipolabili dall'utente
(**nessun rischio di path traversal**), backup semplice, cache HTTP immutabile.
I metadati (nome originale, categoria, tag, licenza, dimensioni) stanno in tabella.

### 4.3 Pacchetti gioco (`.cmpkg`)

Archivio ZIP con `manifest.json` + entità in JSON + cartella `assets/`. Serve per backup,
per spostare un gioco fra desktop e web e per far partire l'utente con Yu-Gi-Oh! già pronto.
Import protetto contro **zip-slip**, con limiti su numero di entry e dimensione decompressa.

---

## 5. Autenticazione, ruoli e bypass locale

**Ruoli:** `Admin` (gestisce giochi, asset, template) e `User` (crea le proprie carte).

**Registrazione a invito.** Con ~10 utenti e l'app esposta su internet, la registrazione libera è un
rischio inutile: l'admin genera inviti a scadenza, non esiste self-signup pubblico.

**Bypass admin in locale** — richiesto per la versione desktop:

- In Photino l'applicazione gira **interamente in-process**: non esiste alcun listener di rete,
  quindi non c'è superficie remota da proteggere.
- Il bypass è un `AuthenticationStateProvider` che restituisce un principal fisso `local-admin`.
- Il codice risiede **esclusivamente nel progetto `CardMaker.Desktop`**: non è compilato
  nell'assembly web, quindi non è attivabile né per errore né per configurazione.
- `CardMaker.Web` esegue all'avvio un **check fail-fast**: se rilevasse un provider di bypass o
  `AppMode=Desktop`, l'avvio viene interrotto (fail-fast, mai fail-open).
- ⚠️ Conseguenza accettata e documentata: chi ha accesso fisico alla macchina è admin
  dell'installazione locale. Il database locale è separato da quello del server.

---

## 6. Sicurezza (checklist OWASP applicata al progetto)

| Rischio | Mitigazione |
|---|---|
| Upload di file malevoli | Solo Admin. Controllo **magic bytes** oltre all'estensione, limite di dimensione, **re-encoding** delle immagini con SkiaSharp (rimuove payload nascosti e metadati) |
| **SVG** | **Bloccati** in upload (vettore di XSS/XXE). Solo raster: PNG, JPG, WEBP |
| Font malevoli | Solo Admin; validazione tramite parsing con SkiaSharp prima del salvataggio; caricati in sandbox |
| Path traversal | Nomi file **content-addressed**, mai derivati dall'input utente |
| Zip-slip (import pacchetti) | Normalizzazione e verifica che ogni path resti sotto la root; limiti su entry/dimensione |
| Code injection nelle regole | **Nessun eval**: condizioni come AST JSON tipizzato |
| XSS | Blazor codifica di default; nessun `MarkupString` su input utente |
| DoS via rendering | Rate limiting, limiti su dimensione carta e numero layer, timeout di render, coda con concorrenza limitata |
| IDOR sulle carte | Ogni query filtra per `OwnerUserId`; gli admin non accedono alle carte altrui per default |
| Accesso diretto agli asset | Serviti da un controller con controllo di autorizzazione, non da `wwwroot` |
| Segreti | Nessuna credenziale nel repo; `user-secrets` in sviluppo, variabili d'ambiente in produzione |
| Registrazione ostile | **Nessun self-signup**: solo inviti generati dall'admin, a scadenza e monouso |
| Brute force sul login | Lockout progressivo, rate limiting per IP e per account, 2FA TOTP opzionale |
| Session hijacking | Cookie `Secure` + `HttpOnly` + `SameSite=Lax`, HTTPS obbligatorio con HSTS |
| Dipendenze vulnerabili | `dotnet list package --vulnerable` in CI, aggiornamenti periodici |

---

## 7. Prestazioni

- Render a 600 DPI (≈ 1400 × 2100 px): obiettivo **< 1,5 s** per carta su hardware consumer.
- Anteprima a ~500 px: obiettivo **< 150 ms** (con cache, ~0 ms).
- Asset decodificati tenuti in una **cache LRU** in memoria (`SKImage`), invalidata per hash.
- Coda di rendering con `SemaphoreSlim` limitata al numero di core.
- Validazione della risoluzione dell'artwork caricato: avviso se sotto la soglia necessaria per i 600 DPI.

---

## 8. Localizzazione

Due livelli distinti:

1. **UI dell'applicazione** → file `.resx` (`it-IT`, `en-US`) con `IStringLocalizer`.
2. **Contenuti definiti dall'admin** (nomi dei giochi, tipi di carta, etichette dei campi, opzioni)
   → colonna JSON `{"it": "...", "en": "..."}` con fallback alla lingua di default del gioco.
3. **Testi statici stampati sulla carta** (es. "ATK", "DEF", "[Magia Continua]") → sono **layer del
   template**, quindi possono essere localizzati definendo varianti di template per lingua oppure
   usando binding a stringhe localizzate del gioco.

---

## 9. Deployment e hardening (l'app è esposta su internet)

La web app è pubblica: anche con 10 utenti, è raggiungibile da chiunque e da qualsiasi bot.
Il livello di protezione va tarato di conseguenza.

### 9.1 Topologia

```
Internet → Reverse proxy (Caddy o Nginx)  →  Kestrel / CardMaker.Web
           • TLS automatico (Let's Encrypt)     • SQLite in WAL mode
           • HSTS, security headers             • asset store su volume dedicato
           • limiti di dimensione richiesta
```

Distribuzione consigliata: **container Docker** su un VPS economico. Con ~10 utenti è più che
sufficiente una singola istanza; il rendering è CPU-bound, quindi contano i core più della RAM.

### 9.2 Checklist di hardening

**Trasporto e header**
- [ ] HTTPS obbligatorio, redirect da HTTP, **HSTS** con preload
- [ ] **Content-Security-Policy** restrittiva (Blazor Server richiede `wasm-unsafe-eval`, non `unsafe-inline`)
- [ ] `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `frame-ancestors 'none'`
- [ ] Antiforgery attivo su tutti i POST

**Identità**
- [ ] Registrazione **solo su invito** (token monouso, a scadenza)
- [ ] Password policy + lockout dopo N tentativi
- [ ] 2FA TOTP opzionale per gli account `Admin`
- [ ] Conferma email obbligatoria

**Abuso di risorse** (il rendering è costoso: è il vettore DoS più realistico)
- [ ] Rate limiting differenziato: login, anteprima, upload, export
- [ ] Quota di spazio per utente e limite al numero di carte
- [ ] Timeout e coda a concorrenza limitata sul renderer
- [ ] Limite di dimensione del body della richiesta a livello di proxy

**Dati**
- [ ] SQLite in **WAL mode** con retry su `SQLITE_BUSY`
- [ ] Backup automatico giornaliero: `VACUUM INTO` per il DB + snapshot dell'asset store
- [ ] Test periodico del **ripristino** (un backup non verificato non è un backup)
- [ ] Contenuti caricati dagli utenti serviti da un **sottodominio separato** senza cookie

**Osservabilità**
- [ ] Logging strutturato (Serilog) senza dati personali eccessivi
- [ ] Audit log delle azioni admin
- [ ] Health check endpoint

### 9.3 Nota su SQLite in produzione

SQLite regge senza problemi ~10 utenti concorrenti, ma ha **un solo scrittore per volta**.
Mitigazioni: WAL mode, transazioni brevi, retry con backoff. Se in futuro la concorrenza in
scrittura diventasse un problema, EF Core rende la migrazione a PostgreSQL una modifica contenuta:
è il motivo per cui l'accesso ai dati passa da repository e non da query sparse nella UI.
