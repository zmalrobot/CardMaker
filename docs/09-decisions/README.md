# Registro delle decisioni architetturali (ADR)

Formato: **contesto → decisione → conseguenze**. Le decisioni superate non si cancellano, si marcano con stato `Superata`.

38 ADR totali (ADR-001 → ADR-038). Suddivisi in 4 file per leggibilità.

---

## Indice

| ID | Titolo | Stato | Fase |
|---|---|---|---|
| [ADR-001](adr-001-to-010.md#adr-001) | Motore di rendering unico e data-driven | Accettata | F0 |
| [ADR-002](adr-001-to-010.md#adr-002) | SkiaSharp come libreria di rendering | Accettata | F0 |
| [ADR-003](adr-001-to-010.md#adr-003) | Rendering server-side anche per l'anteprima | Accettata | F0 |
| [ADR-004](adr-001-to-010.md#adr-004) | Blazor con UI condivisa fra web e desktop | Accettata | F0 |
| [ADR-005](adr-001-to-010.md#adr-005) | Asset su filesystem content-addressed, non nel database | Accettata | F0 |
| [ADR-006](adr-001-to-010.md#adr-006) | Nessuna valutazione di espressioni arbitrarie | Accettata | F0 |
| [ADR-007](adr-001-to-010.md#adr-007) | Versioni di template immutabili | Accettata | F0 |
| [ADR-008](adr-001-to-010.md#adr-008) | Coordinate normalizzate 0..1 | Accettata | F0 |
| [ADR-009](adr-001-to-010.md#adr-009) | Bypass admin locale confinato all'assembly desktop | Accettata | F0 |
| [ADR-010](adr-001-to-010.md#adr-010) | Nessun asset grafico distribuito con l'applicazione | Accettata | F0 |
| [ADR-011](adr-011-to-020.md#adr-011) | Photino.Blazor come host desktop | Accettata | F0 |
| [ADR-012](adr-011-to-020.md#adr-012) | La web app è pubblica: registrazione a invito e hardening completo | Accettata | F0 |
| [ADR-013](adr-011-to-020.md#adr-013) | Solo sRGB, nessuna preparazione per stampa professionale | Accettata | F0 |
| [ADR-014](adr-011-to-020.md#adr-014) | Rush Duel incluso nella v1 | Accettata | F0 |
| [ADR-015](adr-011-to-020.md#adr-015) | Infrastructure dipende da Rendering | Accettata | F0 |
| [ADR-016](adr-011-to-020.md#adr-016) | Le misure derivano dall'invariante, non da arrotondamenti indipendenti | Accettata | F0 |
| [ADR-017](adr-011-to-020.md#adr-017) | Un font per ruolo, non un font per carta | Accettata | F0 |
| [ADR-018](adr-011-to-020.md#adr-018) | Il modello C# è lo schema del layout | Accettata | F1 |
| [ADR-019](adr-011-to-020.md#adr-019) | L'altezza del testo si misura sul quadratone, non sui metrics del font | Accettata | F1 |
| [ADR-020](adr-011-to-020.md#adr-020) | Padding orizzontale e verticale separati | Accettata | F1 |
| [ADR-021](adr-021-to-030.md#adr-021) | I timestamp sono persistiti come tick UTC | Accettata | F1 |
| [ADR-022](adr-021-to-030.md#adr-022) | `symbolRepeater` a griglia fissa e `toggleGroup` a posizioni relative | Accettata | F2 |
| [ADR-023](adr-021-to-030.md#adr-023) | Chiusura F2: richText, overlay, crop a fetta, cache, golden test, PDF, HarfBuzz | Accettata | F2 |
| [ADR-024](adr-021-to-030.md#adr-024) | Regole di selezione template riusano lo stesso AST condizionale del motore | Accettata | F3 |
| [ADR-025](adr-021-to-030.md#adr-025) | Formato pacchetto `.cmpkg` (zip) con protezione Zip-Slip | Accettata | F3 |
| [ADR-026](adr-021-to-030.md#adr-026) | Rush Duel modellato come CardTypes aggiuntivi nello stesso Game Yu-Gi-Oh! | Accettata | F3 |
| [ADR-027](adr-021-to-030.md#adr-027) | Design system su Bootstrap 5.3, token CSS e persistenza tema ibrida | Accettata | F4 |
| [ADR-028](adr-021-to-030.md#adr-028) | Ciclo di vita carte utente, form dinamico, anteprima debouncata ed export multiformato | Accettata | F5 |
| [ADR-029](adr-021-to-030.md#adr-029) | Gestione Contenuti Admin, Schema Editor con Anteprima Live, Operazioni Asset Sicure e Audit Log | Accettata | F6 |
| [ADR-030](adr-021-to-030.md#adr-030) | Studio Template WYSIWYG a 3 Pannelli, Validazione Layout Statica e Versioning Immutabile | Accettata | F7 |
| [ADR-031](adr-031-to-038.md#adr-031) | Host Desktop Cross-Platform Photino.Blazor, Directory di Sistema OS e Bypass Admin Locale Offline | Accettata | F8 |
| [ADR-032](adr-031-to-038.md#adr-032) | Hardening Web, Registrazione Rigorosamente a Invito, Security Headers e Snapshot SQLite Online | Accettata | F9 |
| [ADR-034](adr-031-to-038.md#adr-034) | Estensione Multi-Gioco: Pokémon TCG & Magic: The Gathering | Accettata | F11/F12 |
| [ADR-035](adr-031-to-038.md#adr-035) | Centraggio Ottico CapHeight e Mappatura 1:1 dei Frame Master a Piena Abbondanza | Accettata | F13 |
| [ADR-036](adr-031-to-038.md#adr-036) | Disattivazione Verbosity IPC Photino e Logging Strutturato Sintetico | Accettata | F13 |
| [ADR-037](adr-031-to-038.md#adr-037) | Ottimizzazione Asincrona UI 60 FPS e Hardware Acceleration per Blazor Desktop/Web | Accettata | F13 |
| [ADR-038](adr-031-to-038.md#adr-038) | Transizioni di Pagina Fluide, Feedback Tattile 0ms e Barra di Progresso Reattiva | Accettata | F13 |

> **Nota:** ADR-033 non esiste — la numerazione salta da ADR-032 a ADR-034.

