# CardMaker — Branding & Logo Source of Truth

Questo file documenta la gestione del logo ufficiale e di tutti gli asset grafici derivati all'interno della solution **CardMaker**.

---

## 🎯 1. Source of Truth (Sorgente Ufficiale Unica)

La sorgente grafica ufficiale di CardMaker è memorizzata in:

```text
src/CardMaker.UI/wwwroot/branding/logo.png
```

### Specifiche Tecniche del Master
* **Formato**: PNG a 8-bit/canale RGBA con canale alfa trasparente
* **Risoluzione originale**: `1254 × 1254` pixel
* **Posizionamento Architetturale**: `CardMaker.UI` (Razor Class Library condivisa tra Web e Desktop).
* **URL Statico di Servizio**: `_content/CardMaker.UI/branding/logo.png`

> [!IMPORTANT]
> Non devono esistere duplicati manuali o file indipendenti usati come sorgente grafica. Qualsiasi icona, favicon o formato di sistema operativo deve essere generato a partire da questo master file.

---

## 📦 2. Asset Tecnici Derivati

Tutti i seguenti file sono generati determininisticamente a partire dal master `logo.png`:

### A. Web (`src/CardMaker.Web/wwwroot/`)
* `favicon.ico`: Formato Microsoft Icon multi-risoluzione (16×16, 32×32, 48×48 px)
* `favicon.png`: Favicon standard PNG 32×32 px
* `favicon-16x16.png`: Favicon ad alta fedeltà 16×16 px
* `favicon-32x32.png`: Favicon ad alta fedeltà 32×32 px
* `apple-touch-icon.png`: Icona per dispositivi Apple iOS/iPadOS/macOS (180×180 px)
* `icon-192.png`: Icona PWA / Android per manifest web (192×192 px)
* `icon-512.png`: Icona PWA splash/installazione (512×512 px)
* `site.webmanifest`: Web App Manifest W3C con dichiarazione icone e temi

### B. Desktop (`src/CardMaker.Desktop/`)
* `icon.ico`: Icona dell'assembly Windows eseguibile (`<ApplicationIcon>`) multi-risoluzione (16, 32, 48, 64, 128, 256 px)
* `wwwroot/icon.ico`: Icona per finestra nativa Desktop
* `wwwroot/icon.png`: Icona ad alta risoluzione 512×512 px per splash screen Photino e webview
* `icon.icns`: Pacchetto icone Apple macOS (128, 256, 512, 1024 px con chunk OSType moderni `ic07`, `ic08`, `ic09`, `ic10`)
* `Resources/icons/hicolor/{size}x{size}/apps/cardmaker.png`: Icone per standard FreeDesktop Linux (16, 32, 48, 64, 128, 256, 512 px)
* `Resources/cardmaker.desktop`: File di integrazione desktop Linux FreeDesktop

---

## 🔄 3. Aggiornamento Futuro del Branding

In caso di rinnovo o modifica grafica del logo:

1. Sostituire il file `src/CardMaker.UI/wwwroot/branding/logo.png` con la nuova immagine ad alta risoluzione.
2. Eseguire lo script di generazione:
   ```bash
   python3 scripts/generate-brand-assets.py
   # oppure
   ./scripts/generate-brand-assets.sh
   ```
3. Ricompilare la solution:
   ```bash
   dotnet build CardMaker.slnx
   ```
Tutti gli asset Web, Desktop, manifest e icone di sistema verranno aggiornati automaticamente in modo uniforme e coerente.

