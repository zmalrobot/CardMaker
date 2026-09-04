# Guida operativa

Comandi e informazioni pratiche per lavorare sul progetto.

## Prerequisiti

- **.NET SDK 10.0.400** (verificato su questa macchina)
- ⚠️ `dotnet` **non è nel PATH di sistema**: si trova in `C:\Program Files\dotnet\dotnet.exe`.
  In PowerShell, all'inizio della sessione:
  ```powershell
  $env:Path = "C:\Program Files\dotnet;$env:Path"
  ```

## Script di Avvio Rapido (Clean + Restore + Run)

A livello di root sono disponibili gli script automatizzati per pulire, ripristinare e avviare l'applicazione su qualsiasi OS:

```bash
# Linux / macOS
./run-web.sh        # Avvia l'host Web (ASP.NET Core / Blazor Server)
./run-desktop.sh    # Avvia l'host Desktop nativo (Photino.Blazor)
```

```cmd
:: Windows
run-web.bat         :: Avvia l'host Web (ASP.NET Core / Blazor Server)
run-desktop.bat     :: Avvia l'host Desktop nativo (Photino.Blazor)
```

## Comandi Manuali

```powershell
# Compilare (la solution è in formato .slnx, il nuovo formato di .NET 10)
dotnet build CardMaker.slnx

# Test
dotnet test CardMaker.slnx

# Avviare la web app manualmente
cd src/CardMaker.Web
dotnet run --no-launch-profile --urls http://localhost:5240

# Nuova migrazione EF Core
dotnet ef migrations add <Nome> `
  --project src/CardMaker.Infrastructure `
  --startup-project src/CardMaker.Web `
  --output-dir Persistence/Migrations
```

`dotnet-ef` è installato come **tool locale** (`.config/dotnet-tools.json`): dopo un clone serve
`dotnet tool restore`.

## Primo avvio

Le migrazioni vengono applicate automaticamente all'avvio. Se non esiste alcun amministratore,
viene creato quello configurato in `Bootstrap:AdminEmail` e la password:

- se `Bootstrap:AdminPassword` è valorizzata, viene usata quella;
- altrimenti ne viene **generata una casuale e stampata nei log una sola volta**.

In sviluppo l'email predefinita è `admin@cardmaker.local` (vedi `appsettings.Development.json`).
Per impostare una password in locale senza metterla nel repository:

```powershell
cd src/CardMaker.Web
dotnet user-secrets init
dotnet user-secrets set "Bootstrap:AdminPassword" "<password>"
```

## Dove finiscono i dati

Sotto la cartella indicata da `Storage:DataRoot` (default: `<progetto>/data`, ignorata da git):

```
data/
├─ cardmaker.db          SQLite in modalità WAL
└─ assets/ab/cd/<sha256>.bin   archivio content-addressed
```

Per ripartire da zero basta cancellare la cartella `data`.

## Pagine disponibili

| Rotta | Chi | Cosa |
|---|---|---|
| `/` | Tutti | Home page e panoramica |
| `/cards` | Autenticati | Collezione "Le mie carte" (ordinamento, filtri, duplicazione, eliminazione) |
| `/cards/create` | Autenticati | Wizard di creazione nuova carta con selezione guidata gioco e tipo |
| `/cards/edit/{id}` | Autenticati | Editor carta con form dinamico, selettore tratti, anteprima live 60 FPS ed export multiformato |
| `/guida` | Tutti | Guida utente dettagliata sui campi, sintassi simboli `{sym:...}` e formati supportati |
| `/disclaimer` | Tutti | Note legali e disclaimer non-commerciale fan-made |
| `/design` | Tutti | Galleria e documentazione dei componenti del Design System |
| `/admin/content` | Admin | Gestione CRUD giochi, tipi carta, tratti, simboli e opzioni per gioco |
| `/admin/schema/{id}` | Admin | Editor interattivo dei campi del tipo carta con anteprima live |
| `/admin/templates/{id}` | Admin | Template Studio WYSIWYG a 3 pannelli per la progettazione visiva dei layer |
| `/admin/audit` | Admin | Registro immutabile degli eventi di audit (creazioni, modifiche, export, login) |
| `/admin/invitations` | Admin | Generazione e gestione inviti con token SHA-256 e scadenza |
| `/admin/backups` | Admin | Snapshot online SQLite (`VACUUM INTO`) con verifica integrità (`PRAGMA integrity_check`) |
| `/admin/assets` | Admin | Libreria asset con filtri per gioco, anteprima e sostituzione sicura |
| `/admin/fonts` | Admin | Catalogo font per ruolo, upload TTF/OTF e anteprima campione renderizzata |
| `/admin/placeholders` | Admin | Generatore procedurale frame e simboli segnaposto per tutti i giochi |
| `/admin/render-test` | Admin | Banco di prova interattivo del motore SkiaSharp |
| `/admin/guida` | Admin | Manuale operativo avanzato per amministratori |
| `/Account/Login` | Tutti | Accesso (in Desktop il bypass è automatico in-process) |
| `/Account/Register` | Con Token | Registrazione rigorosamente protetta da token di invito valido |
| `/healthz` | Sistema | Endpoint di probe per Kubernetes / monitoraggio (HTTP 200 OK) |

## Convenzioni di codice e Logging

- Codice, identificatori e commenti in **inglese**; documentazione in **italiano**.
- `TreatWarningsAsErrors` è attivo: qualsiasi avviso blocca la compilazione.
- Le porte (interfacce) risiedono in `CardMaker.Application`; la UI non referenzia mai `CardMaker.Infrastructure`.
- **Logging Pulito**:
  - In Photino Desktop la verbosità IPC è impostata a `0` (`SetLogVerbosity(0)`), evitando qualsiasi dump di dati Base64 su console.
  - I servizi utilizzano `ILogger<T>` strutturato con prefissi standard:
    - `[Preview]` per i render di anteprima (DPI, dimensioni, tempo di calcolo, avvisi).
    - `[Export]` per le esportazioni di file PNG/JPG/PDF.
    - `[Card]` per il ciclo di vita delle carte (creazione, modifica, duplicazione, cancellazione).
    - `[Asset]` per l'upload e la verifica di risorse grafiche e font.
  - Nessun log stampa mai payload di immagini o stringhe Base64.

## Stato del Debito Tecnico

| Voce | Risoluzione |
|---|---|
| Registrazione pubblica senza controllo | **Risolta (F9)**: Registrazione unicamente su invito tramite token SHA-256 a consumo singolo. |
| Pagine Identity non allineate al design system | **Risolta (F4/F10)**: Pagine localizzate in italiano e stilizzate con i token CSS del Design System. |
| Interfaccia e layout scattosi durante il render | **Risolta (F13)**: Pipeline Skia/SQLite incapsulata in `Task.Run` e accelerazione hardware CSS a 60 FPS. |
| Cache delle immagini Skia | **Risolta (F2)**: `IDecodedImageCache` implementata con cache LRU content-addressed per SHA-256. |
| Invio effettivo email di benvenuto | Non bloccante: `IdentityNoOpEmailSender` preservato per semplicità e privacy; i link di invito vengono forniti direttamente dall'interfaccia admin. |
