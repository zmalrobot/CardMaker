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
| `/` | tutti | home |
| `/Account/Login` | tutti | accesso |
| `/admin/assets` | Admin | libreria asset: upload, elenco, anteprima |
| `/admin/fonts` | Admin | font per ruolo: upload, alias, anteprima renderizzata |
| `/admin/placeholders` | Admin | genera i frame segnaposto Yu-Gi-Oh! |
| `/admin/render-test` | Admin | prova interattiva del motore di rendering |
| `/assets/{id}` | autenticati | contenuto di un asset (con autorizzazione, non da `wwwroot`) |
| `/fonts/{id}/preview.png` | Admin | campione di testo renderizzato con quel font |

## Convenzioni di codice

- Codice, identificatori e commenti in **inglese**; documentazione in **italiano**.
- `TreatWarningsAsErrors` è attivo: un avviso blocca la build.
- Le migrazioni EF sono escluse dagli analizzatori tramite
  `src/CardMaker.Infrastructure/Persistence/Migrations/.editorconfig`.
- Le porte (interfacce) stanno in `CardMaker.Application`; la UI non referenzia mai `Infrastructure`.
- **Prima di ricompilare, fermare l'app**: `Get-Process CardMaker.Web | Stop-Process -Force`,
  altrimenti i DLL restano bloccati e la build fallisce con MSB3027.

## Debito tecnico noto

| Voce | Da risolvere in |
|---|---|
| La registrazione pubblica (`/Account/Register`) è ancora attiva: va sostituita dagli inviti | F9 |
| `IdentityNoOpEmailSender`: le email non vengono realmente inviate | F9 |
| Le pagine Identity generate dal template sono in inglese e non seguono il design system | F4 / F10 |
| L'interfaccia usa ancora il Bootstrap di default del template | F4 |
| Nessuna cache dei render: ogni anteprima ricalcola da zero | F2 |
