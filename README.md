# 🃏 CardMaker

**CardMaker** è una piattaforma professionale *data-driven* per la generazione, composizione, rendering e stampa di carte da gioco collezionabili (TCG).

Il progetto è architettato per supportare nativamente molteplici giochi di carte (TCG standard e giapponesi), con pipeline di rendering tipografico ad altissima precisione basata su **SkiaSharp** e conformità agli standard tipografici industriali di bleed, trim, safe zone e risoluzione (150 / 300 / 600 DPI).

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

## 🖼️ Requisiti degli Asset Grafici

Tutti gli asset grafici originali sono di proprietà dei rispettivi autori. L'applicazione non distribuisce materiale protetto da copyright ed è dotata di generatori procedurali di frame e simboli segnaposto (ADR-010).

### Formato File
- **Immagini Frame e Simboli**: Formato PNG a 24 o 32 bit con canale Alpha trasparente (RGBA). Nessun profilo colore CMYK non standard incorporato.
- **Finestra Artwork**: I frame devono avere la finestra dedicata all'illustrazione con canale trasparente al 100% (Alpha = 0).
- **Font**: Formati TrueType (`.ttf`) e OpenType (`.otf`). I font web `.woff2` non sono supportati dal motore di rendering e vengono rifiutati.

---

## 🔣 Sintassi Inline dei Simboli

I campi di testo (come le descrizioni delle abilità, gli effetti e i costi di mana) supportano l'incorporamento dinamico dei glifi grafici tramite token:

```
{sym:<set-key>.<symbol-key>}
```

### Esempi Pratici
- **Yu-Gi-Oh!**: `{sym:attributes.dark}`, `{sym:spell-properties.quick-play}`, `{sym:stars.level}`
- **Pokémon**: `{sym:energies.fire}`, `{sym:energies.water}`, `{sym:energies.lightning}`
- **Magic: The Gathering**: `{sym:mana.tap}`, `{sym:mana.w}`, `{sym:mana.u}`, `{sym:mana.b}`, `{sym:mana.r}`, `{sym:mana.g}`

Il motore tipografico misura l'altezza ottica (*CapHeight*) del font corrente e centra verticalmente i glifi con offset geometrico pari a zero.

---

## 🏗️ Architettura della Solution

Il progetto adotta un'architettura modulare a livelli conforme ai principi della *Clean / Hexagonal Architecture*, compilata con **.NET 10** (C# 13):

```text
CardMaker.slnx
├── src/CardMaker.Domain           # Entità del dominio, aggregati (Card, Template, Asset, Game), Identity e Audit
├── src/CardMaker.Contracts        # DTO, geometrie (CardGeometry), AST condizionale (ConditionOps) e layout JSON
├── src/CardMaker.Application      # Interfacce di servizio (Porte), validatori, logica applicativa e seeder
├── src/CardMaker.Rendering        # Motore SkiaSharp: rasterizzatore, TextEngine (auto-fit), simboli procedurali, PDF
├── src/CardMaker.Infrastructure   # Implementazioni: EF Core SQLite, IAssetStore, FontCatalog, Seeding
├── src/CardMaker.UI               # Razor Class Library (RCL): componenti grafici, editor dinamici, TemplateStudio
├── src/CardMaker.Desktop          # Host nativo multipiattaforma basato su Photino.Blazor (Linux, Windows, macOS)
├── src/CardMaker.Web              # Host Web ASP.NET Core: middleware di sicurezza, rate-limiting, healthcheck
└── tests/                         # Suite automatica: 155 test di unità, integrazione e rendering (100% verdi)
```

---

## ✨ Funzionalità Principali

- 🎴 **Supporto Multi-Gioco Flessibile**: Gestione simultanea di giochi diversi con geometrie, frame e formati di testo dedicati (Yu-Gi-Oh!, Pokémon TCG, Magic: The Gathering).
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
  - Log sintetici ad alta leggibilità per operazioni di anteprima, export, gestione carte e caricamento asset (`[Preview]`, `[Export]`, `[Card]`, `[Asset]`).

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

Attualmente la suite include **155 test** (98 test di rendering/geometria e 57 test applicativi/integrazione), tutti superati con 0 errori e 0 avvisi.

---

## 📚 Documentazione di Progetto (Handover)

Nella cartella [`handover/`](handover/) è disponibile la documentazione tecnica approfondita per sviluppatori e grafici:
- [`handover/STATE.md`](handover/STATE.md): Stato di avanzamento aggiornato e dettagli di versione.
- [`handover/01-card-anatomy.md`](handover/01-card-anatomy.md): Anatomia dettagliata delle carte dei vari giochi.
- [`handover/02-architecture.md`](handover/02-architecture.md): Dettaglio architetturale e pipeline di rendering.
- [`handover/03-data-model.md`](handover/03-data-model.md): Modello dati relazionale e schema JSON dei template.
- [`handover/05-decisions.md`](handover/05-decisions.md): Registro delle decisioni architetturali (ADR-001 → ADR-037).
- [`handover/06-asset-spec.md`](handover/06-asset-spec.md): Specifiche dimensionali per grafici e asset.
- [`handover/07-dev-guide.md`](handover/07-dev-guide.md): Guida per sviluppatori, configurazione e rotte applicative.
- [`handover/08-resume-prompt.md`](handover/08-resume-prompt.md): Prompt di ripristino contesto rapido per nuove sessioni.
