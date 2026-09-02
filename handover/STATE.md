# STATE — Stato corrente del progetto

> **Leggi questo file per primo.** Riassume dove siamo e cosa fare dopo.

**Ultimo aggiornamento:** 2026-09-01
**Fase attiva:** **v1 completata al 100% (F0 → F10)** → pronti ad avviare la **v2: F11 (Pokémon TCG)** e **F12 (Magic: The Gathering)**

> 🖥️ **Cambio di computer in corso.** Questo PC verrà dismesso.
> Per ripartire sulla macchina nuova: [`08-resume-prompt.md`](08-resume-prompt.md) — contiene cosa
> copiare, i prerequisiti e un prompt pronto da incollare in chat.
> Per non esaurire la quota AI: [`09-model-plan.md`](09-model-plan.md).

---

## In una riga

CardMaker è un generatore di carte personalizzate (Yu-Gi-Oh! classico **e Rush Duel** nella v1, poi
Pokémon e Magic) costruito su un **motore di rendering data-driven** in C# / SkiaSharp, con web app
Blazor pubblica su internet + app desktop cross-platform, SQLite, e tutti gli asset grafici caricati
dall'admin.

---

## Cosa è stato fatto

- ✅ Analisi di dominio delle carte dei tre giochi + Rush Duel → [`01-card-anatomy.md`](01-card-anatomy.md)
- ✅ Requisiti confermati dal committente (tutti chiusi) → [`00-project-brief.md`](00-project-brief.md)
- ✅ Architettura tecnica, stack e hardening → [`02-architecture.md`](02-architecture.md)
- ✅ Modello dati e schema del layout → [`03-data-model.md`](03-data-model.md)
- ✅ Roadmap a fasi (F0 → F12) → [`04-roadmap.md`](04-roadmap.md)
- ✅ 31 decisioni architetturali motivate (ADR-001 → ADR-033) → [`05-decisions.md`](05-decisions.md)
- ✅ Specifica asset per il grafico, font inclusi → [`06-asset-spec.md`](06-asset-spec.md)
- ✅ Guida operativa (comandi, primo avvio, dati) → [`07-dev-guide.md`](07-dev-guide.md)
- ✅ Procedura di ripresa su PC nuovo → [`08-resume-prompt.md`](08-resume-prompt.md)
- ✅ Piano dei modelli AI per fase → [`09-model-plan.md`](09-model-plan.md)
- ✅ **F0 — Fondamenta**: solution .NET 10, dominio, SQLite + Identity, asset store, font, segnaposto
- ✅ **F1 — Motore di rendering**: layout data-driven, pipeline a 6 fasi, auto-fit del testo, export PNG/JPEG
- ✅ **F2 — Layer avanzati**: ripetitori, frecce Link, rich text, foil, PDF, golden test
- ✅ **F3 — Contenuti e seed Yu-Gi-Oh!**: grafo di dominio (26 CardType, 28 template), selettore template, formato pacchetto `.cmpkg` con anti-zip-slip
- ✅ **F4 — Design system e temi**: token CSS (palette blu/azzurro chiaro e scuro), shell applicativa responsive, componenti riutilizzabili (`CardPreview`, `ThemeToggle`, `SkeletonLoader`), galleria `/design`
- ✅ **F5 — Flusso utente**: wizard creazione carta, form dinamico condizionale da `FieldDefinition`, editor rich text e simboli, anteprima live debouncata (200 ms), gestione collezione "Le mie carte", duplicazione ed export multiformato PNG/JPG/PDF a 600 DPI
- ✅ **F6 — Admin: gestione contenuti**: CRUD completo giochi, tipi carta, tratti, simboli, opzioni (`/admin/content`), editor dello schema campi (`/admin/schema/{id}`) con anteprima live interattiva, upload multiplo asset, sostituzione blob e safe delete con controllo referenze, registro audit (`/admin/audit`)
- ✅ **F7 — Admin: editor template WYSIWYG**: studio a 3 pannelli (`/admin/templates/{id}`), albero dei layer con z-order e aggiunta rapida layer polimorfi, canvas con viewport, zoom, safe zone (3 mm), bleed (2 mm) e griglia, ispettore proprietà con visual condition builder (`VisibleWhen`), validatore statico del layout e versioning bozze/pubblicate
- ✅ **F8 — Host desktop (Windows, macOS, Linux)**: shell `CardMaker.Desktop` su **Photino.Blazor**, risoluzione percorsi dati di sistema cross-platform (`%LOCALAPPDATA%`, `Application Support`, `.local/share`), bypass admin locale in-process offline (ADR-009, ADR-031), seeding automatico offline, bundle self-contained verificato
- ✅ **F9 — Hardening e messa in produzione**: registrazione rigorosamente a invito con token SHA-256 e blocco fail-closed, security headers, CSP restrittivo, rate limiting sliding window, snapshot online SQLite via `VACUUM INTO` con verifica integrità (`PRAGMA integrity_check`), endpoint `/healthz`, containerizzazione Docker multi-stage + Caddy con TLS automatico
- ✅ **F10 — Rifiniture finali e localizzazione**: pagine Identity completamente localizzate in italiano, pagina note legali e disclaimer fan-made (`/disclaimer`), footer applicativo con badge non-commerciale e crediti

### Cosa esiste nel codice

```
CardMaker.slnx
├─ src/CardMaker.Domain           17 entita' + LocalizedText + AuditLogEntry + Invitation
├─ src/CardMaker.Contracts        CardGeometry + modello del layout + DemoLayouts + ConditionOps
├─ src/CardMaker.Application      porte (asset, font, anteprima, pacchetti, selettore template, card service, export service, admin content service, template admin service, invitation service, backup service) + UploadValidator
├─ src/CardMaker.Rendering        CardRenderer, TextEngine, FontRegistry, segnaposto, PdfExporter
├─ src/CardMaker.Infrastructure   DbContext SQLite, Identity, asset/font store, anteprima, YuGiOhSeedData, GamePackageService, CardService, CardExportService, AdminContentService, TemplateAdminService, InvitationService, BackupService
├─ src/CardMaker.UI               pagine utente (/cards, /cards/create, /cards/edit/{id}), admin (/admin/content, /admin/schema/{id}, /admin/audit, /admin/templates/{id}, /admin/invitations, /admin/backups, asset, font, segnaposto, prova motore), legal (/disclaimer, /terms), componenti form, preview e TemplateStudio
├─ src/CardMaker.Web              host ASP.NET Core per deployment web con security headers, rate limiting, healthcheck e fail-fast check
├─ src/CardMaker.Desktop          host Photino.Blazor cross-platform (Win/macOS/Linux) + bypass admin locale offline
└─ tests/                         95 test verdi (motore) + 46 test verdi (application/desktop/infrastruttura/legal)
```

**Verificato end-to-end dall'interfaccia e dai test:** **v1 completata al 100%**, 141 test verdi, 0 warning, 0 vulnerabilità.

---

## Cosa manca, in sintesi

| Fase | Contenuto | Stato |
|---|---|:--:|
| F0 | Fondamenta: solution, dominio, DB, Identity, asset, font, segnaposto | ✅ |
| F1 | Motore di rendering: nucleo | ✅ |
| F2 | Layer avanzati: ripetitori, frecce Link, rich text, foil, PDF, golden test | ✅ |
| F3 | Contenuti e seed Yu-Gi-Oh! + Rush Duel (26 card type / 28 template) | ✅ |
| F4 | Design system e temi chiaro/scuro su blu e azzurro | ✅ |
| F5 | Flusso utente: wizard, form dinamico, anteprima live, export | ✅ |
| F6 | Admin: gestione contenuti | ✅ |
| F7 | Admin: editor template WYSIWYG | ✅ |
| F8 | Host desktop Windows / macOS / Linux | ✅ |
| F9 | Hardening e messa in produzione | ✅ |
| F10 | Rifiniture finali e localizzazione | ✅ |
| F11 | **v2 — Pokémon TCG** | ⬜ |
| F12 | **v2 — Magic: The Gathering** | ⬜ |

**Al motore mancano 6 tipi di layer** per coprire tutti e tre i giochi: l'elenco completo, con quale
gioco li richiede, è nella sezione *"Sintesi: cosa manca al motore"* di
[`04-roadmap.md`](04-roadmap.md).

---

## Prossimo passo

Avviare la **v2**: **fase F11 — Pokémon TCG** (formato Poker Size 63x88mm, layout a flusso verticale, energia/costi attacco e debolezze/resistenze).
Dettaglio in [`04-roadmap.md`](04-roadmap.md).

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
