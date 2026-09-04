# Guida Operativa per lo Sviluppo

Guida pratica e comandi essenziali per sviluppatori che lavorano sulla repository di **CardMaker**.

---

## 1. Prerequisiti

- **.NET 10 SDK** (versione 10.0.100 o superiore).
- Dipendenze native (su Linux): `libwebkit2gtk-4.1-0`, `libfontconfig1`, `libfreetype6`.

Verifica installazione:
```bash
dotnet --version
```

---

## 2. Script di Avvio Rapido

La root del repository contiene script pronti all'uso con gestione automatica di pulizia binari, ripristino pacchetti e build:

### Linux / macOS
```bash
# Avvio dell'applicazione Desktop nativa (Photino.Blazor)
./run-desktop.sh

# Avvio dell'applicazione Web (Kestrel su http://localhost:5240)
./run-web.sh
```

### Windows
```cmd
:: Avvio Desktop nativo
run-desktop.bat

:: Avvio Web (http://localhost:5240)
run-web.bat
```

---

## 3. Comandi CLI Manuali

```bash
# 1. Ripristino dei tool locali (dotnet-ef)
dotnet tool restore

# 2. Ripristino dei pacchetti NuGet
dotnet restore CardMaker.slnx

# 3. Compilazione della solution (TreatWarningsAsErrors attivo)
dotnet build CardMaker.slnx

# 4. Esecuzione dell'intera suite di collaudo (200 test)
dotnet test CardMaker.slnx

# 5. Avvio manuale host Web
dotnet run --project src/CardMaker.Web/CardMaker.Web.csproj --urls http://localhost:5240

# 6. Avvio manuale host Desktop
dotnet run --project src/CardMaker.Desktop/CardMaker.Desktop.csproj
```

---

## 4. Gestione Database ed EF Core Migrations

Le migrazioni del database vengono applicate automaticamente all'avvio dell'applicazione tramite `DatabaseInitializer`.

Per generare una nuova migrazione dopo modifiche al modello di dominio:
```bash
dotnet ef migrations add <NomeMigrazione> \
  --project src/CardMaker.Infrastructure \
  --startup-project src/CardMaker.Web \
  --output-dir Persistence/Migrations
```

Per applicare le migrazioni manualmente al database locale:
```bash
dotnet ef database update \
  --project src/CardMaker.Infrastructure \
  --startup-project src/CardMaker.Web
```

---

## 5. Primo Avvio e Credenziali di Default

Al primo avvio, se il database non contiene alcun account, viene creato automaticamente l'amministratore con le seguenti credenziali predefinite:
- **Email**: `admin@cardmaker.local`
- **Password**: `Admin123!456` (oppure definita in `Bootstrap:AdminPassword`)

> [!NOTE]
> Nell'host **Desktop** l'autenticazione è gestita dal provider in-process `DesktopAuthenticationStateProvider`: l'utente entra direttamente come amministratore senza necessità di credenziali.

---

## 6. Struttura dei Dati Locali

Tutti i dati generati localmente sono conservati nella directory specificata da `Storage:DataRoot` (predefinita in sviluppo: `<repo>/data` o percorsi utente OS in produzione):

```text
data/
├── CardMaker.db          # Database SQLite in modalità WAL
├── CardMaker.db-wal      # File WAL per letture concorrenti
├── CardMaker.db-shm      # File di memoria condivisa SQLite
└── assets/               # Archivio binario content-addressed (SHA-256)
    └── ab/
        └── cd/
            └── abcdef...bin
```

Per ripristinare completamente l'ambiente da zero è sufficiente eliminare la cartella `data/` o il database. All'avvio successivo il seeder ripopolerà i dati e i template base per tutti i giochi.

---

## 7. Pagine e Rotte Applicative

| Rotta | Accesso | Descrizione |
|---|---|---|
| `/` | Pubblico | Home page di benvenuto e panoramica |
| `/cards` | Autenticato | Collezione "Le mie carte" (filtri, ricerca, duplicazione, eliminazione) |
| `/cards/create` | Autenticato | Creazione guidata nuova carta con selettore gioco e tipo |
| `/cards/edit/{id}` | Autenticato | Editor carta dinamico con anteprima 60 FPS ed export |
| `/guida` | Pubblico | Guida utente sui campi e sintassi token `{sym:...}` |
| `/disclaimer` | Pubblico | Note legali e clausola fan-made non commerciale |
| `/admin/content` | Admin | Gestione CRUD giochi, tipi, tratti, simboli, liste opzioni |
| `/admin/schema/{id}` | Admin | Editor interattivo dello schema campi del tipo carta |
| `/admin/templates/{id}` | Admin | Template Studio WYSIWYG a 3 pannelli per la composizione dei layer |
| `/admin/assets` | Admin | Libreria asset con upload multiplo, filtri per gioco e safe delete |
| `/admin/fonts` | Admin | Catalogo font con registrazione TTF/OTF per alias di ruolo |
| `/admin/placeholders` | Admin | Generatore procedurale di frame e simboli segnaposto |
| `/admin/audit` | Admin | Registro immutabile di audit log |
| `/admin/invitations` | Admin | Gestione inviti e token SHA-256 a tempo |
| `/admin/backups` | Admin | Snapshot online SQLite (`VACUUM INTO`) con integrity check |
| `/admin/render-test` | Admin | Banco di collaudo interattivo per il motore SkiaSharp |
| `/healthz` | Sistema | Endpoint di monitoraggio per container / reverse proxy |
