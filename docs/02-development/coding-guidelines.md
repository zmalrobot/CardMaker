# Linee Guida di Codifica e Standard Architetturali

Tutti i contributi al codice sorgente di **CardMaker** devono rispettare i seguenti principi tecnici e stilistici.

---

## 1. Standard di Linguaggio e Compilazione

- **Target Framework**: .NET 10 (`net10.0`).
- **Versione C#**: C# 13 / 14 con costrutti moderni (Primary Constructors, pattern matching, collection expressions, raw string literals).
- **Trattamento Avvisi**: `TreatWarningsAsErrors = true` è attivo globalmente tramite `Directory.Build.props`. Nessun warning del compilatore o analyzer è consentito.
- **Nullable Reference Types**: Abilitati su tutti i progetti (`<Nullable>enable</Nullable>`). Evitare operatori *null-forgiving* (`!`) non giustificati.

---

## 2. Convenzioni Linguistiche

- **Codice sorgente, identificatori e commenti**: Rigorosamente in lingua **inglese**.
  - Esempi: `CardService`, `CardPreviewRequest`, `ExecuteExportAsync`.
- **Documentazione, guide e messaggi utente Blazor**: In lingua **italiana**.
  - Esempi: "Salvataggio completato", "Libreria Asset".

---

## 3. Rispetto della Clean Architecture

1. **Layering rigoroso**:
   - `CardMaker.Domain` non deve mai referenziare nessun altro progetto della solution.
   - `CardMaker.Contracts` referenzia solo `Domain`.
   - `CardMaker.Application` referenzia solo `Domain` e `Contracts`. Non deve MAI referenziare `Rendering`, `Infrastructure` o `UI`.
   - `CardMaker.Rendering` referenzia solo `Contracts` (non conosce database, HTTP, o EF Core).
   - `CardMaker.UI` non deve mai referenziare `Infrastructure` direttamente; consuma esclusivamente le interfacce esportate da `Application`.
2. **Astrazioni e Porte**:
   - Qualsiasi accesso a risorse esterne (filesystem, database, JSInterop, notifiche) deve essere incapsulato dietro un'interfaccia iniettata in `CardMaker.Application`.

---

## 4. Logging Strutturato e I/O

1. **Prefissi Standard**:
   I log devono utilizzare `ILogger<T>` strutturato con prefissi identificativi:
   - `[Preview]`: rendering anteprima live (DPI, dimensioni, tempo impiegato, esito).
   - `[Export]`: esportazioni raster o PDF (formato, DPI, percorso salvataggio).
   - `[Card]`: operazioni CRUD sul ciclo di vita delle carte.
   - `[Asset]`: caricamento, scansione e rimozione sicura di risorse grafiche o font.
2. **Nessun Dump Binario o Base64**:
   È severamente vietato loggare payload binari, stringhe Base64 o dump IPC.
   In Desktop, la verbosità di Photino è impostata a `0` (`SetLogVerbosity(0)`) per mantenere la console pulita.

---

## 5. Prestazioni, Concorrenza e Risorse Non Gestite

1. **Offload Thread UI**:
   Qualsiasi operazione che richieda elaborazione grafica SkiaSharp o accessi a disco/database all'interno di componenti Razor deve essere delegata a `Task.Run(...)` per preservare la fluidità a 60 FPS dell'interfaccia utente.
2. **Gestione IDisposable**:
   Tutti gli oggetti non gestiti di SkiaSharp (`SKBitmap`, `SKImage`, `SKSurface`, `SKPaint`, `SKTypeface`) devono essere rilasciati deterministicamente tramite blocchi `using` o registrati all'interno di cache con gestione controllata del ciclo di vita.
