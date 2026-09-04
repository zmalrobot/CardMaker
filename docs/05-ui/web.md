# Host Applicativo Web (ASP.NET Core / Kestrel)

L'host web (`CardMaker.Web`) è progettato per la pubblicazione su internet sicura e conforme, destinata a una cerchia ristretta di utenti (~10 utenti contemporanei).

---

## 1. Modello di Esecuzione Blazor Server

L'applicazione web adotta la modalità **Blazor InteractiveServer**:
- Nessun download pesante di assembly WASM al primo accesso client.
- Accesso diretto in-process al database SQLite e al motore di rendering SkiaSharp, eliminando la necessità di esporre API controller REST pesanti per ogni operazione grafica.
- Connessione bidirezionale WebSocket tramite SignalR per aggiornamenti istantanei del DOM.

---

## 2. Hardening e Misure di Sicurezza

Essendo esposta su internet, l'applicazione implementa molteplici livelli di protezione attiva:

### Content Security Policy (CSP)
Intestazioni HTTP conformi e restrittive applicate su tutte le risposte:
```text
default-src 'self';
script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net;
style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net;
img-src 'self' data: blob:;
connect-src 'self' ws: wss:;
frame-ancestors 'none';
```
- Blocco totale di script inline non firmati ed eliminazione di qualsiasi `eval`.
- Protezione contro attacchi di *Clickjacking* via `X-Frame-Options: DENY` e `frame-ancestors 'none'`.

### Rate Limiting Sliding Window
Protezione attiva contro attacchi di forza bruta e Denial of Service (DoS):
- Finestra mobile (*sliding window*) configurata per limitare richieste anomale su endpoint critici di login e generazione anteprime.

### Registrazione Vincolata a Inviti (ADR-030)
- La registrazione aperta al pubblico è completamente disabilitata.
- La pagina `/Account/Register` richiede obbligatoriamente un token di invito valido (`?token=...`).
- Il token viene verificato tramite hashing SHA-256 e invalidato immediatamente al completamento della registrazione.

---

## 3. Monitoraggio e Health Check

L'host espone l'endpoint di stato del sistema:
- **`GET /healthz`**: Restituisce `HTTP 200 OK` con payload `Healthy` se l'host e il database SQLite rispondono correttamente. Utilizzato da Docker, Kubernetes o reverse proxy per verificare la disponibilità del servizio.
