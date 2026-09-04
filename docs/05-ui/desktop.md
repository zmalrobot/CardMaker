# Host Applicativo Desktop (Photino.Blazor)

L'host desktop (`CardMaker.Desktop`) offre un'esperienza utente completamente nativa, indipendente dalla rete e a bassissimo consumo di memoria.

---

## 1. Perché Photino.Blazor anziché Electron o MAUI (ADR-011)

1. **Supporto Linux Reale**: .NET MAUI non supporta Linux e richiede toolchain proprietarie complesse.
2. **Impronta Minima**: Photino non include una copia completa di Google Chromium e Node.js (come Electron). Utilizza il motore web già presente nel sistema operativo:
   - **Linux**: WebKitGTK (`libwebkit2gtk-4.1`).
   - **Windows**: Microsoft Edge WebView2.
   - **macOS**: Apple WebKit nativo.
3. **Eseguibile Leggero e Veloce**: Avvio istantaneo e consumo di memoria ridotto a poche decine di megabyte.

---

## 2. Autenticazione Offline e Bypass Amministratore (ADR-009, ADR-031)

Nel contesto Desktop l'applicazione è monoposto e non richiede password:
- Viene registrato il servizio `DesktopAuthenticationStateProvider`.
- All'avvio genera una `ClaimsPrincipal` fittizia autenticata con:
  - `AuthenticationType = "PhotinoLocalBypass"`
  - `ClaimTypes.Name = "desktop-local-admin"`
  - `ClaimTypes.Role = "Admin"`
- L'utente accede direttamente a tutte le funzioni amministrative (`/admin/*`) e alla gestione carte senza schermate di login.

---

## 3. Gestione File I/O e Finestre di Dialogo OS

A differenza dell'ambiente web, dove i download avvengono tramite il browser:
- L'ambiente desktop inietta `DesktopFileDownloadService`.
- Quando l'utente scarica un asset, un archivio ZIP o una carta esportata, il servizio attiva un dialogo nativo di salvataggio ("Salva con nome..."):
  - Su Linux: tenta l'invocazione out-of-process di `zenity` (o `kdialog`), visualizzando la finestra di dialogo del desktop manager corrente con il percorso `~/Downloads/{nomeFile}` precompilato.
  - Su Windows/macOS: sfrutta le API native del sistema o `PhotinoWindow.ShowSaveFileAsync`.
  - Fallback sicuro: se l'ambiente non supporta finestre di dialogo grafiche interattive, il file viene salvato direttamente nella cartella `~/Downloads`, evitando qualsiasi blocco o perdita di dati.

---

## 4. Integrazione Desktop su Linux (FreeDesktop)

Lo script di avvio `run-desktop.sh` gestisce l'integrazione con i desktop manager Linux (GNOME, KDE, XFCE):
1. Copia le icone multi-risoluzione (16×16 fino a 512×512 px) in `~/.local/share/icons/hicolor/`.
2. Genera o aggiorna il file lanciatore `~/.local/share/applications/cardmaker.desktop`.
3. Inizializza la finestra nativa impostando dimensioni predefinite (`1280 × 850 px`), icona di finestra e titolo dell'applicazione.
