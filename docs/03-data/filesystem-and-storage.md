# Filesystem e Storage degli Asset

CardMaker gestisce tutte le risorse binarie (immagini, texture, font) tramite un'architettura **content-addressed storage** conforme alle direttive di sicurezza e scalabilità (ADR-005).

---

## 1. Architettura Content-Addressed (SHA-256)

I file fisici non vengono mai salvati con il nome originale caricato dall'utente.
All'atto dell'upload:
1. Lo stream binario viene calcolato tramite algoritmo crittografico **SHA-256**.
2. Il file viene memorizzato in una struttura ad albero a due livelli:
   ```text
   assets/
   ├── ab/
   │   └── cd/
   │       └── abcdef0123456789...bin
   ```
3. Il record dell'entità `Asset` nel database conserva l'impronta `Sha256`, il nome originale per il download (`OriginalFileName`), le dimensioni in pixel (`PixelWidth`, `PixelHeight`), il peso e la categoria.

### Vantaggi di Sicurezza e Prestazioni
- **Immunità al Path Traversal**: I percorsi di salvataggio e recupero dipendono esclusivamente dall'hash SHA-256; nessun input utente può influenzare il percorso sul filesystem.
- **Deduplicazione Automatica**: Il caricamento dello stesso file da parte di più template o giochi non duplica lo spazio su disco.
- **Cache Immutabile**: Un file associato a un hash SHA-256 non può mai cambiare contenuto; le risposte HTTP possono includere header `Cache-Control: immutable, max-age=31536000`.

---

## 2. Risoluzione Percorsi per Piattaforma

La classe `DesktopPathResolver` e la configurazione `Storage:DataRoot` determinano la directory radice su ciascun sistema operativo:

| Piattaforma / Host | Percorso Predefinito |
|---|---|
| **Linux Desktop** | `~/.local/share/CardMaker/` |
| **Windows Desktop** | `%LOCALAPPDATA%\CardMaker\` |
| **macOS Desktop** | `~/Library/Application Support/CardMaker/` |
| **Web / Sviluppo** | Directory `<repo>/data/` (configurabile in `appsettings.json`) |

---

## 3. Catalogo Font Tipografici

I font (`FontAsset`) risiedono nello storage content-addressed e vengono registrati nel runtime tipografico `FontRegistry`:
- I font sono catalogati per **alias di ruolo** per ciascun gioco (es. `title`, `stats`, `effect`, `flavor`, `link-rating`).
- In fase di rendering, `FontRegistry` carica deterministicamente i byte in memoria, creando istanze `SKTypeface` cached per evitare accessi ripetuti al disco.
- I formati supportati dal motore tipografico sono rigorosamente **TrueType (`.ttf`)** e **OpenType (`.otf`)**.

---

## 4. Cancellazione Sicura e Integrità Referenziale

L'eliminazione di un asset (`SafeDeleteAssetAsync` in `IAdminContentService`) applica controlli restrittivi:
1. Verifica che nessun layer di nessun template faccia riferimento all'asset.
2. Verifica che nessuna carta salvata referenzi l'asset come artwork o thumbnail.
3. Se l'asset è referenziato, l'eliminazione viene respinta con un messaggio di errore esplicito.
4. Se l'asset non ha riferimenti, viene rimosso dal database e il file binario su disco viene eliminato solo se nessun altro record condivide lo stesso hash SHA-256.
