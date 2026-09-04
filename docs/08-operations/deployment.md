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

## Packaging Desktop (futuro)

La distribuzione Desktop come pacchetto autonomo (senza .NET SDK installato) è pianificata ma non ancora implementata. Le opzioni in valutazione:

- `dotnet publish -r linux-x64 --self-contained` — eseguibile autonomo con runtime embedded (~100 MB).
- Pacchetto `.deb` / `.rpm` per distribuzioni Linux (FreeDesktop `.desktop` già presente in `src/CardMaker.Desktop/Resources/`).
- `AppImage` — distribuzione portabile per Linux senza installazione.
- Windows: MSIX o installer NSIS.
- macOS: bundle `.app` / DMG.

