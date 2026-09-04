# Documentazione CardMaker

Knowledge base tecnica del progetto. Strutturata per essere utile sia agli sviluppatori umani sia alle sessioni AI che riprendono il lavoro senza contesto precedente.

> **Punto di partenza consigliato per nuove sessioni AI:** [`00-overview/project-context.md`](00-overview/project-context.md)

---

## 📂 Struttura

### 00 — Panoramica

| Documento | Contenuto |
|---|---|
| [`project-context.md`](00-overview/project-context.md) | Master context document (17 sezioni) — leggi questo per primo |
| [`project-brief.md`](00-overview/project-brief.md) | Obiettivi, requisiti e scope del progetto |
| [`state-and-roadmap.md`](00-overview/state-and-roadmap.md) | Stato delle fasi F0–F13, roadmap futura |
| [`card-anatomy.md`](00-overview/card-anatomy.md) | Anatomia dettagliata delle carte YGO, Pokémon, MTG |

---

### 01 — Architettura

| Documento | Contenuto |
|---|---|
| [`architecture.md`](01-architecture/architecture.md) | Clean Architecture, pipeline rendering in 6 fasi, multi-host |
| [`projects.md`](01-architecture/projects.md) | Tutti i progetti della solution (7 src + 2 test) |
| [`dependencies.md`](01-architecture/dependencies.md) | Pacchetti NuGet, dipendenze native per OS, strumenti |
| [`branding.md`](01-architecture/branding.md) | Token CSS, palette, convenzioni UI |

---

### 02 — Sviluppo

| Documento | Contenuto |
|---|---|
| [`dev-guide.md`](02-development/dev-guide.md) | Script di avvio, comandi CLI, rotte applicative, migrazioni EF |
| [`coding-guidelines.md`](02-development/coding-guidelines.md) | Standard C# 13, regole di layering, logging |
| [`resume-prompt.md`](02-development/resume-prompt.md) | Prompt di ripristino contesto rapido per nuove sessioni AI |

---

### 03 — Dati

| Documento | Contenuto |
|---|---|
| [`data-model.md`](03-data/data-model.md) | Modello dati relazionale, schema JSON dei template |
| [`filesystem-and-storage.md`](03-data/filesystem-and-storage.md) | Storage content-addressed SHA-256, path resolver |
| [`database-and-migrations.md`](03-data/database-and-migrations.md) | SQLite WAL, EF Core, seeding, VACUUM INTO |

---

### 04 — Logica applicativa

| Documento | Contenuto |
|---|---|
| [`rendering-engine.md`](04-application/rendering-engine.md) | Painters, TextEngine auto-fit, simboli procedurali, PDF |
| [`card-services.md`](04-application/card-services.md) | CardService, CardExportService, IFileDownloadService |
| [`admin-and-content.md`](04-application/admin-and-content.md) | ContentManager, SchemaEditor, TemplateStudio WYSIWYG |

---

### 05 — UI

| Documento | Contenuto |
|---|---|
| [`ui-architecture.md`](05-ui/ui-architecture.md) | RCL condivisa, token CSS, componenti, ThemeToggle |
| [`desktop.md`](05-ui/desktop.md) | Host Photino.Blazor, dialoghi file nativi, offline bypass |
| [`web.md`](05-ui/web.md) | Host ASP.NET Core, CSP, rate limiting, sistema inviti |

---

### 06 — Testing

| Documento | Contenuto |
|---|---|
| [`testing-strategy.md`](06-testing/testing-strategy.md) | 200 test totali, due progetti, filtri, golden test |
| [`test-hardening-report.md`](06-testing/test-hardening-report.md) | Report test hardening (40+ nuovi test aggiunti) |

---

### 07 — Performance

| Documento | Contenuto |
|---|---|
| [`performance-architecture.md`](07-performance/performance-architecture.md) | Task.Run, cache LRU, LOH, IPC noise, ottimizzazioni UI |
| [`performance-analysis.md`](07-performance/performance-analysis.md) | Analisi completa dei bottleneck (811 righe) |
| [`optimization-report.md`](07-performance/optimization-report.md) | Report implementazione ottimizzazioni |

---

### 08 — Operazioni

| Documento | Contenuto |
|---|---|
| [`configuration.md`](08-operations/configuration.md) | appsettings, Storage:DataRoot, Bootstrap secrets |
| [`deployment.md`](08-operations/deployment.md) | Docker multi-stage, Caddy TLS, run scripts |
| [`troubleshooting.md`](08-operations/troubleshooting.md) | Problemi noti e workaround (download WebKitGTK, woff2, ecc.) |

---

### 09 — Decisioni architetturali (ADR)

| Documento | Contenuto |
|---|---|
| [`README.md`](09-decisions/README.md) | Indice di tutti i 38 ADR |
| [`adr-001-to-010.md`](09-decisions/adr-001-to-010.md) | ADR-001 → ADR-010 (fondamenta del progetto) |
| [`adr-011-to-020.md`](09-decisions/adr-011-to-020.md) | ADR-011 → ADR-020 (piattaforme, sicurezza, tipografia) |
| [`adr-021-to-030.md`](09-decisions/adr-021-to-030.md) | ADR-021 → ADR-030 (layer avanzati, template, design system) |
| [`adr-031-to-038.md`](09-decisions/adr-031-to-038.md) | ADR-031 → ADR-038 (desktop, hardening, performance UI) |

---

### 10 — Riferimento

| Documento | Contenuto |
|---|---|
| [`asset-spec.md`](10-reference/asset-spec.md) | Specifica asset grafici per il grafico (frame, simboli, font, foil) |
| [`model-plan.md`](10-reference/model-plan.md) | Guida alla scelta del modello AI per fase di sviluppo |
| [`glossary.md`](10-reference/glossary.md) | Glossario: termini TCG, abbreviazioni, formule mm/px |

---

## 🔑 Guida rapida per nuove sessioni AI

1. Leggi [`00-overview/project-context.md`](00-overview/project-context.md) — contiene tutto il contesto critico.
2. Leggi [`02-development/resume-prompt.md`](02-development/resume-prompt.md) — prompt strutturato per riprendere il lavoro.
3. Se stai lavorando su un'area specifica, vai direttamente alla sezione corrispondente.
4. Per le decisioni architetturali, consulta [`09-decisions/README.md`](09-decisions/README.md).
5. Per i problemi noti, consulta [`08-operations/troubleshooting.md`](08-operations/troubleshooting.md).

> **Regola linguistica**: codice, identificatori e commenti in **inglese**. Documentazione e testi utente in **italiano**.

