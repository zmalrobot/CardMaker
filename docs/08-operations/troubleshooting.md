# Troubleshooting

Problemi noti, cause e workaround.

---

## 1. Pulsante "Scarica" non funziona in modalità Desktop

**Sintomo:** Cliccando "Scarica" nella Libreria Asset (`/admin/assets`) o "Esporta" nel form carta, non accade nulla. Nessun errore in console.

**Causa:** Il pattern `<a download>` + `a.click()` JavaScript funziona correttamente nei browser web, ma WebKitGTK (la WebView di Photino su Linux) **ignora silenziosamente** i download avviati via JavaScript blob URL. Non è un bug del codice: è una limitazione di piattaforma della WebView.

**Percorso codice coinvolto:**
- `AssetLibrary.razor` → `DownloadAssetAsync()` → `JS.InvokeVoidAsync("cardMaker.downloadFile", ...)`
- `theme.js:52` → `cardMaker.downloadFile()` → `a.download` + `a.click()` → **ignorato da WebKitGTK**

**Soluzione pianificata (non ancora implementata):** Introduzione dell'interfaccia `IFileDownloadService` con due implementazioni:
- `WebFileDownloadService` — mantiene il pattern JS blob esistente (funziona nel browser).
- `DesktopFileDownloadService` — usa `PhotinoWindow.ShowSaveFileAsync()` (dialogo nativo GTK) con fallback a `~/Downloads/`.

Vedi `docs/04-application/card-services.md` per il design dell'interfaccia.

---

## 2. Font `.woff2` rifiutati in upload

**Sintomo:** L'upload di un file font `.woff2` nella Libreria Asset viene rifiutato con un errore di validazione.

**Causa:** SkiaSharp non supporta il formato WOFF2 (Web Open Font Format). I file vengono rifiutati a monte per evitare che vengano registrati nel database ma risultino illeggibili al renderer (ADR-017).

**Soluzione:** Convertire il font nel formato `.ttf` o `.otf` prima dell'upload. Strumenti gratuiti:
- `woff2` CLI: `woff2_decompress font.woff2` → produce `font.ttf`
- FontForge (GUI): Apri il `.woff2`, esporta come TTF.
- [fontsquirrel.com/tools/webfont-generator](https://www.fontsquirrel.com/tools/webfont-generator) (online)

---

## 3. SQLite — lock su operazioni a lunga esecuzione

**Sintomo:** Durante export di molte carte o backup (`VACUUM INTO`), altre operazioni in scrittura possono ricevere `SQLiteBusy` o risultare lente.

**Causa:** SQLite in WAL mode permette letture concorrenti illimitate, ma ammette una sola scrittura alla volta. Le operazioni `VACUUM INTO` acquisiscono un lock di lettura esclusivo per la durata della copia.

**Workaround:**
- Il backup è accessibile da `/admin/backups` e viene eseguito tramite `VACUUM INTO` che produce una copia coerente senza bloccare le letture.
- Se si verificano timeout in scrittura, aumentare `CommandTimeout` in `appsettings.json` (default: 30s).
- Non eseguire backup manuali (`sqlite3 .backup`) mentre l'applicazione è in esecuzione: usare sempre il comando integrato.

---

## 4. WebKitGTK — prestazioni blur basse (risolto)

**Sintomo:** (Storico — risolto in F13/ADR-037) In modalità Desktop su Linux, l'animazione dello spinner durante il rendering mostrava frame rate basso (10-15 FPS) con micro-freeze.

**Causa:** Il CSS `backdrop-filter: blur(6px)` sull'overlay di caricamento costringeva WebKitGTK a ricalcolare la sfocatura su tutta la finestra ad ogni frame dell'animazione, esaurendo la GPU.

**Soluzione applicata:** Rimosso `backdrop-filter: blur(6px)`, sostituito con `background: rgba(13, 17, 23, 0.72)` e `transform: translateZ(0)` per hardware acceleration. Animazioni ora fluide a 60 FPS.

---

## 5. IPC Photino — dump Base64 in console (risolto)

**Sintomo:** (Storico — risolto in F13/ADR-036) La console dell'app Desktop stampava centinaia di KB di caratteri ogni volta che veniva aggiornata un'anteprima, rendendo il terminale inutilizzabile.

**Causa:** Il livello di verbosità IPC predefinito di Photino.Blazor stampava ogni messaggio scambiato tra .NET e la WebView, incluse le stringhe Base64 delle immagini PNG.

**Soluzione applicata:** `app.MainWindow.SetLogVerbosity(0)` in `CardMaker.Desktop/Program.cs`.

---

## 6. Build fallisce con `TreatWarningsAsErrors`

**Sintomo:** La build fallisce con un warning trattato come errore (es. CA2201, CS8600, ecc.).

**Causa:** `TreatWarningsAsErrors` è attivo globalmente nel progetto. Qualsiasi avviso del compilatore o di analisi statica blocca la build.

**Soluzione:** Risolvere il warning prima di committare. Non aggiungere `#pragma warning disable` senza commento esplicativo che giustifichi la soppressione.

---

## 7. `dotnet` non trovato nel PATH (Windows)

**Sintomo:** `run-desktop.bat` o `run-web.bat` restituisce `❌ Errore: .NET SDK non trovato nel PATH.`

**Causa:** Su Windows, il PATH viene aggiornato all'installazione di .NET ma richiede la riapertura del terminale (o logout/login).

**Soluzione:**
1. Aprire un nuovo terminale (cmd o PowerShell) dopo aver installato .NET 10 SDK.
2. Verificare: `dotnet --version`
3. Se non trovato, aggiungere manualmente al PATH: `C:\Program Files\dotnet\`

---

## 8. Database vuoto dopo il primo avvio Web

**Sintomo:** L'app Web parte ma nessun gioco o template è disponibile.

**Causa:** Il seeding automatico avviene solo se il database è completamente vuoto. Se il database esiste già (anche parzialmente) il seeding viene saltato.

**Soluzione:** Se si vuole ripartire da zero:
```bash
# Fermare l'applicazione, poi:
rm /app/data/CardMaker.db          # o il percorso configurato in Storage__DataRoot
# Riavviare — il seeding verrà eseguito automaticamente
```

> [!CAUTION]
> L'eliminazione del database cancella **tutti i dati**: carte, template, asset, utenti. Eseguire un backup prima.

---

## 9. Errore `SQLITE_CANTOPEN` all'avvio

**Sintomo:** L'app crasha all'avvio con `SqliteException: unable to open database file`.

**Cause possibili:**
1. La directory `Storage__DataRoot` non esiste e non può essere creata (permessi).
2. Il path configurato è errato (es. variabile d'ambiente non espansa).
3. In Docker: il volume `/app/data` non è montato correttamente.

**Soluzione:**
```bash
# Verifica permessi (Docker)
docker exec cardmaker-web ls -la /app/data

# Verifica variabile d'ambiente
docker exec cardmaker-web printenv Storage__DataRoot

# Desktop Linux — verifica percorso standard
ls -la ~/.local/share/CardMaker/
```

---

## 10. Seeding Pokémon / MTG — font non trovati

**Sintomo:** Le anteprime di Pokémon o Magic mostrano il font di fallback (Roboto) invece dei font dedicati.

**Causa:** I font incorporati (GillSans, Beleren, MPlantin, ecc.) sono embedded in `CardMaker.Infrastructure` come risorse. Se l'assembly non viene caricato correttamente o il seeder non ha registrato i font alias, il `FontService` cade sul fallback.

**Diagnosi:**
- Controllare i log all'avvio: devono apparire righe `[Font] Registrato alias ...` per ogni gioco.
- Navigare a `/admin/fonts` e verificare che i ruoli font per Pokémon e MTG siano presenti.

**Soluzione:** Se i font alias mancano, forzare il re-seeding eliminando il database e riavviando.

