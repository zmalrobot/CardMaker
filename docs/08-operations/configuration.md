# Configurazione Applicativa ed Environment Variables

CardMaker adotta il sistema standard di configurazione di **ASP.NET Core / Microsoft.Extensions.Configuration**.

---

## 1. File di Configurazione e Precedenza

I valori di configurazione vengono caricati con il seguente ordine di priorità (l'ultimo sovrascrive i precedenti):
1. `appsettings.json` (valori base di default).
2. `appsettings.{Environment}.json` (es. `appsettings.Development.json` o `appsettings.Production.json`).
3. .NET User Secrets (in ambiente di sviluppo locale).
4. Variabili d'ambiente del sistema operativo.
5. Argomenti da riga di comando.

---

## 2. Parametri di Configurazione Chiave

### `ConnectionStrings`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=data/CardMaker.db"
  }
}
```
* **Significato**: Stringa di connessione a SQLite.
* **Variabile d'ambiente equivalente**: `ConnectionStrings__DefaultConnection`.

### `Storage`
```json
{
  "Storage": {
    "DataRoot": "data"
  }
}
```
* **Significato**: Directory radice per l'archiviazione del database (`CardMaker.db`), degli asset binari content-addressed (`assets/`), dei font (`fonts/`), dei modelli AI GGUF (`models/`) e della configurazione AI locale (`ai-settings.json`).
* **Comportamento Desktop**: Nell'host `CardMaker.Desktop`, se non specificata, viene determinata automaticamente da `DesktopPathResolver` puntando alla cartella dati applicativa standard dell'utente (`~/.local/share/CardMaker`, `%LOCALAPPDATA%\CardMaker`, `~/Library/Application Support/CardMaker`).
* **Variabile d'ambiente equivalente**: `Storage__DataRoot`.

### Configurazione AI Locale (`ai-settings.json`)
La configurazione del motore generativo multimodale `CardMaker.AI` è persistita automaticamente nel file `<Storage:DataRoot>/ai-settings.json`:
```json
{
  "IsEnabled": true,
  "SelectedModelKey": "Auto",
  "CpuThreads": 0,
  "CustomModelsDirectory": null,
  "IsImageGenerationEnabled": true,
  "SelectedImageModelKey": "Auto"
}
```
* `IsEnabled`: flag globale per abilitare/disabilitare la generazione assistita di testi ed effetti (LLM).
* `SelectedModelKey`: modello linguistico attivo tra quelli supportati (`"Auto"`, `"gemma-2-2b"`, `"gemma-3-4b"`, `"gemma-2-9b"`, `"gemma-2-27b"`). Se impostato su `"Auto"`, viene selezionato in base alla RAM fisica rilevata all'avvio.
* `CpuThreads`: numero di thread CPU dedicati all'inferenza (0 per allocazione automatica).
* `CustomModelsDirectory`: percorso facoltativo per una directory modelli personalizzata alternativa alla sottocartella predefinita `models/`.
* `IsImageGenerationEnabled`: flag globale per abilitare/disabilitare la generazione di illustrazioni ed artwork (Diffusione).
* `SelectedImageModelKey`: modello di diffusione attivo tra quelli supportati (`"Auto"`, `"sd-1.5-turbo"`, `"dreamshaper-8"`, `"sdxl-lightning-4step"`). Se impostato su `"Auto"`, viene calibrato sulla memoria di sistema e sulle capacità grafiche.


### `Bootstrap` (Inizializzazione Amministratore)
```json
{
  "Bootstrap": {
    "AdminEmail": "admin@cardmaker.local",
    "AdminPassword": "Admin123!456"
  }
}
```
* **Significato**: Credenziali utilizzate da `DatabaseInitializer` per creare l'account amministratore se la tabella utenti è vuota.
* **Sicurezza in Produzione**: Impostare `Bootstrap__AdminPassword` tramite variabile d'ambiente sicura o Secret Manager; se omessa, viene generata una password casuale stampata nei log una sola volta all'avvio.

---

## 3. Gestione Secrets in Sviluppo

Per non committare credenziali o configurazioni sensibili nel repository Git:
```bash
cd src/CardMaker.Web
dotnet user-secrets init
dotnet user-secrets set "Bootstrap:AdminPassword" "LaMiaPasswordSicura!"
```
