# Prompt di Ripresa Lavoro per Nuove Sessioni

Questo documento contiene le istruzioni e il prompt pronto da incollare quando si apre una nuova sessione di lavoro o si trasferisce il progetto su una nuova macchina.

---

## 1. Trasferimento su una Nuova Macchina

Se cloni o trasferisci la repository su un nuovo computer:

1. **Prerequisiti**:
   - .NET 10 SDK installato.
   - Dipendenze grafiche native su Linux:
     ```bash
     sudo apt-get update && sudo apt-get install -y libwebkit2gtk-4.1-0 libfontconfig1 libfreetype6
     ```
2. **Restore e Verifica**:
   ```bash
   dotnet tool restore
   dotnet restore CardMaker.slnx
   dotnet build CardMaker.slnx
   dotnet test CardMaker.slnx
   ```
3. **Avvio Rapido**:
   - `./run-desktop.sh` (host desktop native)
   - `./run-web.sh` (host web su http://localhost:5240)

---

## 2. Prompt Pronto per Inizializzare Nuove Sessioni AI

Copia e incolla il blocco sottostante per fornire immediatamente all'assistente AI il contesto completo e aggiornato:

```text
Stai lavorando sul progetto "CardMaker", un generatore professionale data-driven di carte collezionabili (TCG) per Yu-Gi-Oh! (classico e Rush Duel), Pokémon TCG e Magic: The Gathering.

Stack e Architettura:
- .NET 10 (C# 13), Clean Architecture modulare: Domain, Contracts, Application, Rendering, Infrastructure, UI (RCL), Desktop (Photino.Blazor), Web (ASP.NET Core).
- Rendering: SkiaSharp server-side a 150/300/600 DPI, sistema full-bleed master canvas, auto-fit tipografico (shrink/condense), centraggio ottico su CapHeight e simboli procedurali per mana MTG ed energie Pokémon.
- Database: SQLite in WAL mode con EF Core. Seeding automatico all'avvio con credenziali admin: admin@cardmaker.local / Admin123!456. In Desktop l'accesso admin è automatico offline.
- Prestazioni: Rendering asincrono su Task.Run, UI a 60 FPS, verbosità Photino impostata a 0 per azzerare lo spam IPC base64 in console.
- Test: 200 test automatizzati di unità, integrazione e rendering, tutti verdi (dotnet test CardMaker.slnx).

Knowledge Base:
Prima di iniziare qualsiasi modifica o proporre soluzioni, consulta la documentazione centrale nella cartella `docs/`:
- `docs/00-overview/project-context.md`: Quadro sintetico e master context.
- `docs/00-overview/state-and-roadmap.md`: Stato attuale v2 e storico delle fasi F0-F13.
- `docs/01-architecture/architecture.md`: Pipeline di rendering e flussi a livelli.
- `docs/01-architecture/projects.md`: Descrizione di tutti i 7 progetti della solution.
- `docs/03-data/data-model.md`: Modello dati, entità e schema JSON dei template.
- `docs/09-decisions/README.md`: Registro delle 38 decisioni architetturali (ADR).
- `docs/02-development/dev-guide.md`: Comandi operativi e rotte applicative.

Regole da rispettare:
- Codice e commenti in inglese; documentazione e comunicazioni in italiano.
- Avvisi del compilatore trattati come errori (TreatWarningsAsErrors).
- Nessun eval né codice hardcodato per-gioco: l'architettura è rigorosamente data-driven.
```
