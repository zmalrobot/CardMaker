# Deployment

## Modalità di esecuzione

CardMaker supporta due modalità di esecuzione indipendenti, entrambe condividono lo stesso codice UI e motore di rendering.

| Modalità | Host | Multiutente | Porta di rete | Storage |
|---|---|---|---|---|
| **Desktop** | Photino.Blazor | No (singolo admin locale) | Nessuna | `~/.local/share/CardMaker/` (Linux) |
| **Web** | ASP.NET Core (Kestrel) | Sì (inviti) | 8080 (Docker) / 5240 (dev) | `/app/data/` (Docker volume) |

---

## Avvio rapido — Sviluppo locale

### Script pronti all'uso

| Script | Sistema | Comando |
|---|---|---|
| `run-desktop.sh` | Linux / macOS | `./run-desktop.sh` |
| `run-web.sh` | Linux / macOS | `./run-web.sh` |
| `run-desktop.bat` | Windows | `run-desktop.bat` |
| `run-web.bat` | Windows | `run-web.bat` |

Ogni script esegue nell'ordine:
1. Verifica presenza di `.NET 10 SDK` nel PATH.
2. `dotnet clean CardMaker.slnx` — pulizia binari precedenti.
3. `dotnet restore CardMaker.slnx` — ripristino pacchetti NuGet.
4. *(solo Linux, solo Desktop)* Registrazione icone e launcher FreeDesktop in `~/.local/share/`.
5. `dotnet run --project src/CardMaker.{Desktop|Web}/...`

### Avvio manuale

```bash
# Desktop
dotnet run --project src/CardMaker.Desktop/CardMaker.Desktop.csproj

# Web (porta 5240, ambiente Development)
ASPNETCORE_ENVIRONMENT=Development dotnet run \
  --project src/CardMaker.Web/CardMaker.Web.csproj \
  --no-launch-profile --urls "http://localhost:5240"
```

### Credenziali predefinite (Web / primo avvio)

| Campo | Valore |
|---|---|
| Email | `admin@cardmaker.local` |
| Password | `Admin123!456` |

In modalità Desktop l'accesso admin è automatico (nessun login richiesto).

---

## Deployment produzione — Docker + Caddy

### Struttura dei file

```
CardMaker/
├── Dockerfile              # Multi-stage build per CardMaker.Web
├── docker-compose.yml      # Stack completo: web + Caddy reverse proxy
└── Caddyfile               # Configurazione TLS automatico (Let's Encrypt)
```

### Dockerfile — dettaglio

Il `Dockerfile` è multi-stage:

| Stage | Immagine base | Azione |
|---|---|---|
| `build` | `mcr.microsoft.com/dotnet/sdk:10.0` | Restore NuGet + `dotnet publish -c Release` |
| `final` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Runtime minimale, utente non-root |

**Librerie native installate nel runtime** (necessarie per SkiaSharp su Linux):
- `libfontconfig1` — risoluzione font di sistema
- `libfreetype6` — rendering font FreeType
- `fonts-dejavu-core` — font di fallback per render senza asset caricati
- `curl` — health check probe

**Variabili d'ambiente esposte:**

| Variabile | Valore predefinito | Scopo |
|---|---|---|
| `ASPNETCORE_URLS` | `http://+:8080` | Porta Kestrel interna |
| `Storage__DataRoot` | `/app/data` | Directory database + asset |

**Volume dichiarato:** `/app/data` — persistente, mappato a `cardmaker-data` in docker-compose.

### docker-compose.yml — stack completo

```
cardmaker-web    →  build locale, porta 8080 interna
caddy            →  reverse proxy, porte 80 + 443, TLS Let's Encrypt automatico
```

Servizi e routing:
- `cardmaker-web` non espone porte direttamente verso l'host.
- `caddy` gestisce TLS, redirect HTTP→HTTPS e proxy pass verso `cardmaker-web:8080`.
- Rete interna: `cardmaker-net` (bridge).

### Variabili d'ambiente richieste per la produzione

Creare un file `.env` nella root del repository (non committare):

```dotenv
DOMAIN_NAME=cardmaker.example.com
ACME_EMAIL=admin@example.com
BOOTSTRAP_ADMIN_EMAIL=admin@cardmaker.local
BOOTSTRAP_ADMIN_PASSWORD=<password-sicura>
```

> [!IMPORTANT]
> `BOOTSTRAP_ADMIN_PASSWORD` viene letta solo al **primo avvio** (database vuoto). Impostare una password robusta prima di avviare il container in produzione.

### Comandi Docker

```bash
# Build e avvio stack completo
docker compose up -d --build

# Verifica stato
docker compose ps
docker compose logs -f cardmaker-web

# Aggiornamento (rebuild + restart rolling)
docker compose up -d --build --no-deps cardmaker-web

# Stop
docker compose down

# Backup manuale del volume dati
docker run --rm -v cardmaker_cardmaker-data:/data -v $(pwd):/backup \
  alpine tar czf /backup/backup-$(date +%Y%m%d).tar.gz -C /data .
```

### Health check

L'endpoint `/healthz` esegue una probe sul database SQLite. Docker lo interroga ogni 30s; Caddy può usarlo come upstream health check:

```bash
curl http://localhost:8080/healthz
# risposta attesa: 200 OK, body "Healthy"
```

---

## Caddyfile — configurazione TLS

Il `Caddyfile` configura Caddy come reverse proxy con TLS automatico tramite ACME/Let's Encrypt:

```caddy
{$DOMAIN_NAME} {
    tls {$ACME_EMAIL}
    reverse_proxy cardmaker-web:8080
}
```

Le variabili `{$DOMAIN_NAME}` e `{$ACME_EMAIL}` vengono espanse dall'ambiente Docker.

> [!TIP]
> Per un ambiente staging senza dominio pubblico, impostare `DOMAIN_NAME=localhost` e aggiungere `tls internal` per un certificato auto-firmato gestito da Caddy.

---

## Prerequisiti di sistema

### Desktop (sviluppo + produzione)

| Requisito | Versione minima | Note |
|---|---|---|
| .NET SDK | 10.0 | [Download](https://dotnet.microsoft.com/download) |
| WebKitGTK | 4.1+ | Solo Linux — installato con il DE |
| `zenity` o `kdialog` | qualsiasi | Solo Linux — dialoghi file nativi |

**Linux — verifica prerequisiti:**
```bash
dotnet --version          # deve mostrare 10.x.x
pkg-config --modversion webkit2gtk-4.1
which zenity || which kdialog
```

### Web (Docker)

| Requisito | Versione minima |
|---|---|
| Docker Engine | 24.0+ |
| Docker Compose | v2.x |
| Dominio DNS pubblico | per TLS Let's Encrypt |

---

## Packaging Desktop e Distribuzione Multi-Piattaforma

La distribuzione Desktop e Web come pacchetti autonomi e guidati è implementata e integrata nel workflow GitHub Actions [`.github/workflows/release.yml`](../../.github/workflows/release.yml):

### 1. Windows
- **Installer Guidato (.exe)**: Generato tramite Inno Setup ([`packaging/windows/installer.iss`](../../packaging/windows/installer.iss)), con procedura d'installazione moderna, scelta percorso, icone Start/Desktop e disinstallatore registrato nel sistema.
- **Portatile Self-Contained (.zip)**: Estrabile ed eseguibile senza installazione né prerequisiti (runtime .NET 10 incorporato).
- **Portatile Framework-Dependent (.zip)**: Versione compatta (~15 MB) per ambienti con .NET 10 già installato.

### 2. Linux
- **Pacchetto Nativo Debian/Ubuntu (.deb)**: Assemblato con lo script [`packaging/linux/build-deb.sh`](../../packaging/linux/build-deb.sh) tramite `dpkg-deb`. Installa i binari in `/usr/lib/cardmaker/`, il launcher `/usr/bin/cardmaker`, l'icona a 512px e registra l'applicazione con FreeDesktop in `/usr/share/applications/cardmaker.desktop`. Include le dipendenze native dichiarate (`libwebkit2gtk-4.1-0`, `libfontconfig1`, `libfreetype6`).
- **Tarball Portatile Universale (.tar.gz)**: Archivio compresso contenente i binari Self-Contained e lo script di avvio `run.sh`, compatibile con qualsiasi distribuzione Linux desktop.

### 3. Pipeline di Pubblicazione GitHub Actions
Al push di un tag di versione `v*` (o dispatch manuale), il job `publish-release`:
- Raccoglie tutti gli artefatti prodotti dai runner `windows-latest` e `ubuntu-latest`.
- Calcola i checksum crittografici SHA-256 (`SHA256SUMS.txt`).
- Pubblica automaticamente la GitHub Release corredata di note di rilascio e file scaricabili.


