# Servizi Applicativi e Ciclo di Vita delle Carte

I servizi di `CardMaker.Application` gestiscono la logica di business, l'orchestrazione delle esportazioni e il calcolo dei valori delle carte.

---

## 1. Ciclo di Vita della Carta (`ICardService`)

L'interfaccia `ICardService` incapsula tutte le operazioni eseguibili dall'utente sulla propria collezione:
- **`CreateCardAsync`**: Validazione iniziale dei valori rispetto allo schema campi (`FieldDefinition`), generazione dell'identificativo univoco `Guid`, associazione all'utente autenticato e salvataggio nel database.
- **`UpdateCardAsync`**: Aggiornamento atomico dei campi modificati preservando lo storico e la versione del template originario.
- **`DuplicateCardAsync`**: Clonazione completa della carta con titolo suffissato da " (Copia)", utile per creare varianti di mazzo rapidamente.
- **`DeleteCardAsync`**: Eliminazione sicura della carta appartenente all'utente autenticato con dereferenziazione della thumbnail cache.
- **`GetUserCardsAsync`**: Recupero della lista sintetica delle carte dell'utente con filtri per gioco, ordinamento temporale e paginazione.

---

## 2. Esportazione Multiformato (`ICardExportService`)

Il servizio `CardExportService` gestisce la generazione del file finale per la stampa o l'archiviazione:
- **Opzioni di Esportazione (`CardExportOptions`)**:
  - `Format`: `RenderFormat.Png`, `RenderFormat.Jpg`, `RenderFormat.Pdf`.
  - `Dpi`: 150 (anteprima web), 300 (stampa standard), 600 (alta fedeltà tipografica).
  - `IncludeBleed`: se abilitato, include l'abbondanza perimetrale di 2 mm per tipografia; se disabilitato, ritaglia esattamente sulla linea di taglio (*Trim Box*).
  - `Face` / `BothFaces`: consente di esportare solo il fronte, solo il retro o entrambi (nel caso del PDF, generando un documento a 2 pagine).
- **Parallelizzazione Concorrente (`CON-001`)**:
  - Il caricamento delle risorse da database e storage è eseguito in modo sequenziale thread-safe tramite `RenderResourceLoader`.
  - La rasterizzazione del fronte e del retro viene eseguita in parallelo sfruttando `Task.WhenAll`, riducendo significativamente i tempi di generazione dei documenti fronte/retro.

---

## 3. Calcolo Valori Derivati (`CardDerivedValuesService`)

Per evitare logiche condizionali cablate nella UI o nei componenti Razor, `CardDerivedValuesService` calcola deterministicamente le proprietà derivate in base al gioco:
- **Yu-Gi-Oh!**:
  - Formattazione riga dei tipi mostro: es. `[Drago / Effetto]` o `[Guerriero / Fusione / Effetto]`.
  - Calcolo del valore `link_rating` pari al numero di frecce Link attivate.
  - Normalizzazione delle statistiche ATK/DEF (es. `?` per valori indefiniti).
- **Pokémon TCG**:
  - Calcolo del testo di evoluzione: `"Evolve da {pre-evolution}"`.
- **Magic: The Gathering**:
  - Composizione della riga tipale: es. `"Creatura Leggendaria — Drago"`.
  - Calcolo del costo di mana totale e formattazione con token `{sym:...}`.

---

## 4. Servizio di Download Astratto (`IFileDownloadService`)

L'astrazione `IFileDownloadService` unifica il salvataggio dei file binari prodotti (esportazioni carte, singoli asset, archivi ZIP) distinguendo per host:
- In ambiente **Desktop**: apre la finestra di dialogo nativa di salvataggio del sistema operativo (`zenity` / `kdialog` / `PhotinoWindow.ShowSaveFileAsync`) e scrive direttamente lo stream su disco senza overhead Base64.
- In ambiente **Web**: invoca il download manager del browser client tramite JSInterop (`cardMaker.downloadFile`).
