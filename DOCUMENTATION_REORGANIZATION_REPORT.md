# DOCUMENTATION_REORGANIZATION_REPORT.md

## Riorganizzazione documentazione Markdown — Report completo

**Data:** 2026-09-04  
**Stato:** ✅ Completata

---

## Obiettivo

Riorganizzare tutta la documentazione Markdown esistente nel repository in una knowledge base coerente,
ordinata e navigabile, utile sia per sviluppatori umani sia per future sessioni AI che non abbiano
contesto precedente.

---

## Struttura creata

```
docs/
├── README.md                            ← Hub di navigazione master
├── 00-overview/
│   ├── project-context.md               ← Master context (17 sezioni) — punto di partenza AI
│   ├── project-brief.md                 ← Obiettivi, requisiti, scope
│   ├── state-and-roadmap.md             ← Fasi F0–F13, roadmap futura
│   └── card-anatomy.md                  ← Anatomia carte YGO/Pokémon/MTG
├── 01-architecture/
│   ├── architecture.md                  ← Clean Architecture, pipeline, multi-host
│   ├── projects.md                      ← 7 src + 2 test projects
│   ├── dependencies.md                  ← NuGet, dipendenze native, strumenti
│   └── branding.md                      ← Token CSS, palette, design system
├── 02-development/
│   ├── dev-guide.md                     ← Script, CLI, rotte, migrazioni EF
│   ├── coding-guidelines.md             ← C# 13, layering, logging
│   └── resume-prompt.md                 ← Prompt ripristino contesto AI (aggiornato)
├── 03-data/
│   ├── data-model.md                    ← Modello dati relazionale, JSON layout
│   ├── filesystem-and-storage.md        ← SHA-256 content-addressed, path resolver
│   └── database-and-migrations.md       ← WAL, EF Core, seeding, VACUUM INTO
├── 04-application/
│   ├── rendering-engine.md              ← Painters, TextEngine, simboli procedurali, PDF
│   ├── card-services.md                 ← CardService, CardExportService, IFileDownloadService
│   └── admin-and-content.md             ← ContentManager, SchemaEditor, TemplateStudio
├── 05-ui/
│   ├── ui-architecture.md               ← RCL, token CSS, componenti, ThemeToggle
│   ├── desktop.md                       ← Photino host, dialoghi nativi, offline bypass
│   └── web.md                           ← Kestrel, CSP, rate limiting, inviti
├── 06-testing/
│   ├── testing-strategy.md              ← 200 test, filtri, golden test
│   └── test-hardening-report.md         ← Report 40+ nuovi test aggiunti
├── 07-performance/
│   ├── performance-architecture.md      ← Task.Run, cache LRU, LOH, IPC, UI 60fps
│   ├── performance-analysis.md          ← Analisi bottleneck completa (811 righe)
│   └── optimization-report.md           ← Report implementazione ottimizzazioni
├── 08-operations/
│   ├── configuration.md                 ← appsettings, DataRoot, Bootstrap secrets
│   ├── deployment.md                    ← Docker, Caddy TLS, run scripts, packaging
│   └── troubleshooting.md               ← 10 problemi noti con cause e workaround
├── 09-decisions/
│   ├── README.md                        ← Indice tutti i 38 ADR
│   ├── adr-001-to-010.md                ← ADR-001 → ADR-010
│   ├── adr-011-to-020.md                ← ADR-011 → ADR-020
│   ├── adr-021-to-030.md                ← ADR-021 → ADR-030
│   └── adr-031-to-038.md                ← ADR-031 → ADR-038
└── 10-reference/
    ├── asset-spec.md                    ← Specifica asset grafici per il grafico
    ├── model-plan.md                    ← Piano modelli AI per fase
    └── glossary.md                      ← Glossario TCG, formule mm/px, abbreviazioni
```

**Totale: 36 file Markdown.**

---

## File eliminati

| File / Cartella | Ragione |
|---|---|
| `handover/` (12 file) | Sostituita da `docs/` — contenuto migrato, aggiornato e riorganizzato |
| `PERFORMANCE_ANALYSIS.md` | Copiato in `docs/07-performance/performance-analysis.md` |
| `PERFORMANCE_OPTIMIZATION_REPORT.md` | Copiato in `docs/07-performance/optimization-report.md` |
| `CODE_OPTIMIZATION_ANALYSIS.md` | Contenuto consolidato nella documentazione performance |
| `CODE_OPTIMIZATION_IMPLEMENTATION_REPORT.md` | Contenuto consolidato nella documentazione performance |
| `TEST_HARDENING_REPORT.md` | Copiato in `docs/06-testing/test-hardening-report.md` |

---

## File aggiornati

| File | Modifica |
|---|---|
| `README.md` (root) | Test count 155 → **200** (107 rendering + 93 applicativi). Link `handover/` → `docs/`. Titolo sezione aggiornato. |

---

## Invarianti rispettate

- ✅ `README.md` root non spostato né eliminato — solo contenuto aggiornato.
- ✅ Nessun file `.cs`, `.csproj`, `.sln`, `.slnx` modificato.
- ✅ Il file `src/CardMaker.UI/wwwroot/branding/README.md` è stato mantenuto nel suo percorso originale (è parte del codice sorgente, non documentazione pura).

---

## Miglioramenti rispetto alla struttura precedente

| Prima (`handover/`) | Dopo (`docs/`) |
|---|---|
| 12 file flat in ordine numerico | 36 file organizzati in 11 categorie tematiche |
| File mischiati con report al root | Report integrati nella sezione pertinente |
| ADR tutti in un file da 817 righe | ADR suddivisi in 4 file + indice separato |
| Nessun glossario | Glossario completo con formule mm/px e termini TCG |
| Nessuna sezione operations | `deployment.md` + `troubleshooting.md` completi |
| Link incrociati non aggiornati | Tutti i link puntano ai percorsi `docs/` corretti |
| Test count errato (155) | Conteggio corretto: **200** test |

