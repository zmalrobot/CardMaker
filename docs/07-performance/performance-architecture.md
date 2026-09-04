# Architettura delle Performance e Pipeline 60 FPS

CardMaker è ingegnerizzato per garantire reattività istantanea dell'interfaccia utente (60 FPS) ed elevato throughput sia durante la composizione interattiva sia durante le esportazioni di massa a 600 DPI.

---

## 1. Il Principio dell'Offload Asincrono (ADR-037)

In Blazor (sia in modalità InteractiveServer che Photino Desktop in-process), il ciclo di vita dei componenti Razor e il rendering del DOM condividono il thread di sincronizzazione principale.
Se un'operazione pesante su CPU (come la rasterizzazione SkiaSharp o la decodifica di bitmap ad alta risoluzione) viene eseguita in modo sincrono sul thread della UI, l'applicazione subisce evidenti blocchi (*freezing*), calo drastico di framerate e mancata risposta agli input.

### Strategia Applicata
Tutte le operazioni CPU-intensive e di I/O pesante sono delegate al thread pool:
```csharp
// Esecuzione disaccoppiata del rendering
var renderResult = await Task.Run(() => _renderer.Render(request, resources), cancellationToken);
```
- Il thread UI rimane costantemente libero per gestire input utente, transizioni CSS e aggiornamenti degli spinner.
- Il debounce dell'anteprima (200 ms) cancella tempestivamente le richieste in volo tramite `CancellationTokenSource`, evitando calcoli superflui se l'utente continua a digitare.

---

## 2. Gerarchia delle Cache in Memoria

1. **`DecodedImageCache` (Cache Immagini SkiaSharp)**:
   - Cache LRU (*Least Recently Used*) per memorizzare le istanze `SKImage` decodificate a partire dall'hash SHA-256 dell'asset.
   - Protezione del ciclo di vita con `disposeOnEviction: false`: impedisce che una bitmap venga deallocata prematuramente mentre una superficie di disegno parallela la sta utilizzando (`CON-003`, `MEM-001`).
2. **`FontBytesCache` e `TypefaceCache`**:
   - Cache singleton in memoria per i byte dei file di font letti dal disco (`FontService`), eliminando letture ripetute su filesystem (`FS-PERF-003`).
   - Mantenimento delle istanze `SKTypeface` e `SKFont` native tra i vari passaggi di rendering tipografico.
3. **Cache Layout Deserializzati**:
   - `CardPreviewService` memorizza in una cache LRU le istanze `CardTemplateLayout` già validate, azzerando il costo di parsing JSON ad ogni ciclo di anteprima (`SER-PERF-002`).
4. **Cache Condizioni Form**:
   - `DynamicCardForm` memorizza i delegati e le condizioni `VisibleWhen` valutate, evitando riflessione ed enumerazioni ad ogni pressione di tasto (`SER-PERF-003`).

---

## 3. Gestione della Memoria e Large Object Heap (LOH)

Per prevenire frammentazione della memoria e pause di Garbage Collection (GC) prolungate:
- **Zero-Copy e Pre-Sizing**: In `PdfExporter`, i `MemoryStream` vengono pre-dimensionati in base alle dimensioni note delle pagine e i dati raster vengono passati direttamente via `SKData` senza duplicazioni (`CPU-PERF-002`, `MEM-PERF-005`).
- **Dimensione Buffer I/O**: Stream di lettura e scrittura su filesystem utilizzano buffer calibrati (4096 byte / 81920 byte) per evitare allocazioni oltre la soglia dell'LOH (85.000 byte) (`MEM-PERF-001`).
- **Post-Processing a Zero Copia**: Eliminato l'encoding intermedio in PNG durante il ritaglio del bleed; la superficie sorgente viene disegnata direttamente sulla superficie di destinazione con matrice affine (`CPU-PERF-001`).

---

## 4. Azzeramento del Rumore IPC Console (ADR-036)

Nell'applicazione Desktop su Photino:
- La comunicazione tra la WebView nativa e il runtime C# avviene tramite un canale IPC interno.
- L'impostazione `app.MainWindow.SetLogVerbosity(0)` disattiva il dump di debug raw del canale, eliminando l'emissione in console di stringhe Base64 pesanti megabyte.
- I log applicativi utilizzano `ILogger<T>` strutturato con prefissi compatti (`[Preview]`, `[Export]`, `[Card]`, `[Asset]`).
