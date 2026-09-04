# 08 — Prompt di Ripresa Lavoro (Handover)

Questo documento contiene le istruzioni e il prompt pronto da incollare quando si apre una nuova sessione di lavoro o si trasferisce il progetto su una nuova macchina.

---

## 💻 Trasferimento su una Nuova Macchina

Se cloni o copi la repository su un nuovo computer:

1. **Prerequisiti di Sistema**:
   - **.NET 10 SDK** (o versione superiore).
   - Su Linux (Ubuntu/Debian): librerie grafiche e WebKit per Photino Desktop:
     ```bash
     sudo apt-get update && sudo apt-get install -y libwebkit2gtk-4.1-0 libfontconfig1 libfreetype6
     ```
2. **Ripristino e Build**:
   ```bash
   dotnet tool restore
   dotnet restore
   dotnet build
   dotnet test
   ```
3. **Avvio Rapido**:
   - `./run-desktop.sh` per l'host desktop.
   - `./run-web.sh` per l'host web (porta predefinita `5240`).

---

## 📋 Prompt Pronto da Incollare nella Nuova Sessione

Copia e incolla il blocco sottostante per inizializzare immediatamente qualsiasi assistente AI sul progetto:

```text
Stai lavorando sul progetto "CardMaker", un generatore professionale data-driven di carte collezionabili (TCG) per Yu-Gi-Oh! (classico e Rush Duel), Pokémon TCG e Magic: The Gathering.

Stack e Architettura:
- .NET 10 (C# 13), Clean Architecture modulare: Domain, Contracts, Application, Rendering, Infrastructure, UI (RCL), Desktop (Photino.Blazor), Web (ASP.NET Core).
- Rendering: SkiaSharp server-side a 150/300/600 DPI, sistema full-bleed master canvas, auto-fit tipografico (shrink/condense), centraggio ottico su CapHeight e simboli procedurali SVG/Skia per mana MTG ed energie Pokémon.
- Database: SQLite in WAL mode con EF Core. Seeding automatico all'avvio con credenziali admin: admin@cardmaker.local / Admin123!456. In Desktop l'accesso admin è automatico offline.
- Prestazioni: Rendering asincrono su Task.Run, UI hardware-accelerated a 60 FPS, verbosità Photino impostata a 0 per azzerare lo spam IPC base64 da console.
- Test: 155 test di unità e rendering, tutti verdi (dotnet test).

Cartella Handover:
Prima di iniziare qualsiasi modifica o proporre soluzioni, consulta i documenti di progetto nella cartella `handover/`:
- `handover/STATE.md`: Stato corrente e attività concluse.
- `handover/01-card-anatomy.md`: Anatomia e regole visive dei 3 giochi.
- `handover/02-architecture.md`: Pipeline di rendering e principi architetturali.
- `handover/03-data-model.md`: Modello dati ed entità.
- `handover/04-roadmap.md`: Registro delle fasi F0-F12 completate.
- `handover/05-decisions.md`: Registro decisioni architetturali (ADR-001 -> ADR-037).
- `handover/07-dev-guide.md`: Comandi, rotte applicative e guide pratiche.

Rispetta le convenzioni di progetto:
- Codice e commenti in inglese; documentazione e comunicazioni all'utente in italiano.
- Avvisi del compilatore trattati come errori (TreatWarningsAsErrors).
- Nessun eval o codice hardcodato per-gioco: l'architettura è rigorosamente data-driven.
```
