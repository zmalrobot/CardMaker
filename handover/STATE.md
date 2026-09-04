# STATE — Stato corrente del progetto

> **Leggi questo file per primo.** Riassume dove siamo e cosa fare dopo.

**Ultimo aggiornamento:** 2026-09-04
**Fase attiva:** **v2 completata al 100% (F0 → F12) + Ottimizzazioni UX & Performance**: Yu-Gi-Oh! (classico + Rush Duel), Pokémon TCG, e Magic: The Gathering pienamente operativi con rendering, font, simboli procedurali, allineamenti calibrati, pipeline asincrona a 60 FPS e logging pulito.

---

## In una riga

CardMaker è un generatore di carte personalizzate per **Yu-Gi-Oh!** (classico e Rush Duel), **Pokémon TCG** e **Magic: The Gathering**, costruito su un **motore di rendering data-driven** in C# / SkiaSharp, con web app Blazor e app desktop cross-platform, SQLite, e gestione completa di asset, font e template.

---

## Cosa è stato fatto

- ✅ Analisi di dominio delle carte dei tre giochi + Rush Duel → [`01-card-anatomy.md`](01-card-anatomy.md)
- ✅ Requisiti confermati dal committente → [`00-project-brief.md`](00-project-brief.md)
- ✅ Architettura tecnica, stack e hardening → [`02-architecture.md`](02-architecture.md)
- ✅ Modello dati e schema del layout → [`03-data-model.md`](03-data-model.md)
- ✅ Roadmap a fasi (F0 → F12) → [`04-roadmap.md`](04-roadmap.md)
- ✅ Decisioni architetturali motivate (ADR-001 → ADR-038) → [`05-decisions.md`](05-decisions.md)
- ✅ Specifica asset per il grafico, font inclusi → [`06-asset-spec.md`](06-asset-spec.md)
- ✅ Guida operativa (comandi, primo avvio, dati) → [`07-dev-guide.md`](07-dev-guide.md)
- ✅ Prompt di ripresa rapida per nuove sessioni → [`08-resume-prompt.md`](08-resume-prompt.md)
- ✅ **F0 — Fondamenta**: solution .NET 10, dominio, SQLite + Identity, asset store, font, segnaposto
- ✅ **F1 — Motore di rendering**: layout data-driven, pipeline a 6 fasi, auto-fit del testo, export PNG/JPEG
- ✅ **F2 — Layer avanzati**: ripetitori, frecce Link, rich text, foil, PDF, golden test
- ✅ **F3 — Contenuti e seed Yu-Gi-Oh!**: grafo di dominio (26 CardType, 28 template), selettore template, formato pacchetto `.cmpkg` con anti-zip-slip
- ✅ **F4 — Design system e temi**: token CSS (palette blu/azzurro chiaro e scuro), shell applicativa responsive, componenti riutilizzabili (`CardPreview`, `ThemeToggle`, `SkeletonLoader`), galleria `/design`
- ✅ **F5 — Flusso utente**: wizard creazione carta, form dinamico condizionale da `FieldDefinition`, editor rich text e simboli, anteprima live debouncata (200 ms), gestione collezione "Le mie carte", duplicazione ed export multiformato PNG/JPG/PDF a 600 DPI
- ✅ **F6 — Admin: gestione contenuti**: CRUD completo giochi, tipi carta, tratti, simboli, opzioni (`/admin/content`), editor dello schema campi (`/admin/schema/{id}`) con anteprima live interattiva, upload multiplo asset filtrabili per gioco, sostituzione blob e safe delete con controllo referenze, registro audit (`/admin/audit`)
- ✅ **F7 — Admin: editor template WYSIWYG**: studio a 3 pannelli (`/admin/templates/{id}`), albero dei layer con z-order e aggiunta rapida layer polimorfi, canvas con viewport, zoom, safe zone (3 mm), bleed (2 mm) e griglia, ispettore proprietà con visual condition builder (`VisibleWhen`), validatore statico del layout e versioning bozze/pubblicate
- ✅ **F8 — Host desktop (Windows, macOS, Linux)**: shell `CardMaker.Desktop` su **Photino.Blazor**, risoluzione percorsi dati di sistema cross-platform (`%LOCALAPPDATA%`, `Application Support`, `.local/share`), bypass admin locale in-process offline (ADR-009, ADR-031), seeding automatico offline, bundle self-contained verificato
- ✅ **F9 — Hardening e messa in produzione**: registrazione rigorosamente a invito con token SHA-256 e blocco fail-closed, security headers, CSP restrittivo, rate limiting sliding window, snapshot online SQLite via `VACUUM INTO` con verifica integrità (`PRAGMA integrity_check`), endpoint `/healthz`, containerizzazione Docker multi-stage + Caddy con TLS automatico
- ✅ **F10 — Rifiniture finali e localizzazione**: pagine Identity completamente localizzate in italiano, pagina note legali e disclaimer fan-made (`/disclaimer`), footer applicativo con badge non-commerciale e crediti
- ✅ **F11 — v2: Pokémon TCG**: Poker Size (63x88mm), seeding completo (Base, Fase 1, Fase 2, EX, GX, V, VMAX, Trainer, Energia), simboli procedurali energia (Erba, Fuoco, Acqua, Lampo, Psico, Lotta, Oscurità, Metallo, Incolore), font incorporati (`GillSansBold`, `GillSansItalic`, `GillSans`, `Futura-Bold`), layout demo e test visivi
- ✅ **F12 — v2: Magic: The Gathering**: Poker Size (63x88mm), seeding completo (Creatura, Planeswalker, Istantaneo, Stregoneria, Incantesimo, Artefatto, Terra), simboli procedurali di mana ({W}, {U}, {B}, {R}, {G}, {C}, numeri 0-9, {X}, {T}), simboli rarità (Comune, Non comune, Rara, Mitica), font incorporati (`Beleren2016-Bold`, `Beleren2016SmallCaps-Bold`, `Mplantin`), layout demo e test visivi
- ✅ **Allineamento & Calibrazione Tipografica**: algoritmo di centraggio ottico su `CapHeight` in `CardRenderer`, mappatura automatica 1:1 dei frame master a piena abbondanza (eliminazione compressione bleed nel trim), calibrazione delle altezze di caselle e testi per evitare sovrapposizioni e clipping dei tratti
- ✅ **UI 60 FPS & Offload Asincrono (ADR-037)**: Rimozione `backdrop-filter: blur` in Linux WebKitGTK, accelerazione GPU su overlay di caricamento (`will-change: transform`), incapsulamento completo di SQLite e SkiaSharp in `Task.Run(...)` per garantire reattività istantanea dell'interfaccia.
- ✅ **Correzione Navigazione & Selezione Voci**: Risolto bug di selezione congiunta di *Le mie carte* e *Nuova carta* tramite `Match="NavLinkMatch.All"` in `DesktopNavMenu.razor` e `NavMenu.razor`.
- ✅ **Logging Strutturato & Azzeramento Spam Base64 (ADR-036)**: `SetLogVerbosity(0)` in Photino per silenziare il dump raw del canale IPC e adozione di log sintetici essenziali per `[Preview]`, `[Export]`, `[Card]` e `[Asset]`.
- ✅ **Transizioni di Pagina Fluide & Feedback Tattile 0ms (ADR-038)**: Wrapper `@key="NavigationManager.Uri"` con animazione `cm-page-enter` a 60 FPS, micro-feedback tattile `:active` (scale 0.98), progress bar superiore reattiva e reveal morbido dei dati.

### Cosa esiste nel codice

```
CardMaker.slnx
├─ src/CardMaker.Domain           17 entita' + LocalizedText + AuditLogEntry + Invitation
├─ src/CardMaker.Contracts        CardGeometry + modello del layout + DemoLayouts + ConditionOps
├─ src/CardMaker.Application      porte e seeder (YuGiOh, Pokemon, MTG, asset, font, anteprima, pacchetti, selettore template, card service, export service, admin content service, template admin service, invitation service, backup service) + UploadValidator
├─ src/CardMaker.Rendering        CardRenderer, TextEngine, FontRegistry, segnaposto procedurali (YuGiOh, Pokemon, MTG), generatori di simboli procedurali, PdfExporter
├─ src/CardMaker.Infrastructure   DbContext SQLite, Identity, asset/font store con font TTF/OTF embedded per tutti i giochi, anteprima, Content & Font Seeder per Yu-Gi-Oh!, Pokémon e MTG
├─ src/CardMaker.UI               pagine utente (/cards, /cards/create, /cards/edit/{id}, /guida), admin (/admin/content, /admin/schema/{id}, /admin/audit, /admin/templates/{id}, /admin/invitations, /admin/backups, /admin/assets, /admin/fonts, /admin/placeholders, /admin/render-test, /admin/guida), componenti form dinamici, preview e TemplateStudio
├─ src/CardMaker.Web              host ASP.NET Core per deployment web con security headers, rate limiting, healthcheck e fail-fast check
├─ src/CardMaker.Desktop          host Photino.Blazor cross-platform (Win/macOS/Linux) + bypass admin locale offline
└─ tests/                         155 test verdi (98 rendering + 57 application/integration), 0 warning, 0 errori
```

---

## Tabella delle Fasi

| Fase | Contenuto | Stato |
|---|---|:--:|
| F0 | Fondamenta: solution, dominio, DB, Identity, asset, font, segnaposto | ✅ |
| F1 | Motore di rendering: nucleo | ✅ |
| F2 | Layer avanzati: ripetitori, frecce Link, rich text, foil, PDF, golden test | ✅ |
| F3 | Contenuti e seed Yu-Gi-Oh! + Rush Duel (26 card type / 28 template) | ✅ |
| F4 | Design system e temi chiaro/scuro su blu e azzurro | ✅ |
| F5 | Flusso utente: wizard, form dinamico, anteprima live, export | ✅ |
| F6 | Admin: gestione contenuti e asset filtrati per gioco | ✅ |
| F7 | Admin: editor template WYSIWYG | ✅ |
| F8 | Host desktop Windows / macOS / Linux | ✅ |
| F9 | Hardening e messa in produzione | ✅ |
| F10 | Rifiniture finali e localizzazione | ✅ |
| F11 | **v2 — Pokémon TCG** | ✅ |
| F12 | **v2 — Magic: The Gathering** | ✅ |
| F13 | **Rifinitura UX, 60 FPS Asincrono & Logging Pulito** | ✅ |

---

## Prossimo passo

Il core del sistema e tutte le fasi pianificate della v2 sono complete e stabili.
Le attività future opzionali / di mantenimento includono:
1. **Asset Grafici Artistici**: Sostituzione facoltativa dei frame e simboli procedurali con asset grafici ad alta risoluzione disegnati da artisti (utilizzando [`06-asset-spec.md`](06-asset-spec.md)).
2. **Supporto Giochi Aggiuntivi**: Eventuale estensione a One Piece Card Game o Lorcana seguendo il pattern modulare consolidato (Domain Seeder + Font Seeder + Simboli procedurali).
3. **Distribuzione Installer Nativo**: Confezionamento di installer MSIX per Windows, `.deb`/AppImage per Linux e `.dmg` per macOS.

**In parallelo, lavoro non bloccante:** produzione degli asset e dei font seguendo
[`06-asset-spec.md`](06-asset-spec.md). Bastano 4 font e 2 frame per vedere una carta credibile.

---

## Decisioni chiave da ricordare

1. **Un solo motore, data-driven.** Nessuna logica per-gioco nel codice.
2. **SkiaSharp** per raster e PDF: `SKFont.ScaleX` è ciò che rende credibile il testo Yu-Gi-Oh!.
3. **L'anteprima usa lo stesso renderer dell'export** → parità garantita.
4. **Coordinate normalizzate 0..1** → indipendenza dal DPI.
5. **Nessun `eval`**: le regole condizionali sono un AST JSON tipizzato.
6. **Versioni di template immutabili**: una carta salvata non cambia aspetto da sola.
7. **Nessun asset grafico distribuito con l'app**: carica tutto l'admin.
8. **Photino.Blazor** per il desktop (MAUI escluso perché non supporta Linux).
9. **Web pubblica → registrazione solo su invito** e hardening completo (fase F9).
10. **Solo sRGB**: niente CMYK, niente preparazione per stampa professionale.
11. **Rush Duel non deve richiedere nuovi tipi di layer**: e' il test di validita' del design.
12. **Le misure delle carte stanno in `CardGeometry`** e sono coperte da test: master = trim + 2 × abbondanza.
13. **Ogni elemento di testo ha il proprio font**, risolto per alias di ruolo (ADR-017).
14. **L'altezza del testo si misura sul quadratone**, non su ascent+descent (ADR-019).
15. `dotnet` **non e' nel PATH**: vedi [`07-dev-guide.md`](07-dev-guide.md).

---

## Requisiti chiusi

| Domanda | Risposta |
|---|---|
| Esistono già gli asset Yu-Gi-Oh!? | **No** — verranno prodotti su specifica → `06-asset-spec.md` |
| Quali sistemi desktop? | **Windows, macOS e Linux** → Photino.Blazor |
| La web app è esposta su internet? | **Sì** → hardening completo, registrazione a invito |
| Serve la stampa professionale? | **No** → solo sRGB, niente CMYK né crocini |
| Serve il Rush Duel? | **Sì** → incluso nella v1 |

## Decisioni rinviate

| # | Decisione | Quando |
|---|---|---|
| 1 | Rush Duel come `CardType` dello stesso `Game` o come `Game` separato | **Risolta in F3**: `CardType` aggiuntivi nello stesso `Game` `yugioh` (ADR-026) |
| 2 | Sezioni Rush (`[REQUISITO]`/`[EFFETTO]`) come campi distinti o come `repeatingBlock` | **Risolta in F2**: marker `[LABEL]` dentro `richText`, nessun layer dedicato (ADR-023) |
| 3 | Piano B su Linux se WebKitGTK dà problemi: server locale + browser di sistema | F8 |
| 4 | Dove ospitare la web app (VPS, provider, dominio) | F9 |
| 5 | Sostituire la registrazione pubblica con il flusso a inviti | F9 |
| 6 | Simboli di mana MTG: un asset per combinazione o composizione a runtime | F12 |

---

## Convenzioni di lavoro

- Lingua della documentazione: **italiano**. Codice, identificatori e commenti: **inglese**.
- Al termine di ogni fase: aggiornare `STATE.md` e `04-roadmap.md`; se sono state prese decisioni non
  banali, aggiungere un ADR in `05-decisions.md`.
- Nessun segreto o credenziale nei file di handover.
