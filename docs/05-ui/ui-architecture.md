# Architettura dell'Interfaccia Utente (CardMaker.UI)

L'interfaccia utente è interamente sviluppata all'interno della Razor Class Library **`CardMaker.UI`**, condivisa ed eseguita senza duplicazioni sia nell'host Web che nell'host Desktop.

---

## 1. Design System e Token CSS

Il Design System si basa su una personalizzazione calibrata di **Bootstrap 5.3.3**, governata da variabili CSS semantiche (token) definite in `wwwroot/css/cardmaker-theme.css`:

```css
:root {
    --cm-primary: #0284c7;       /* Blu principale CardMaker */
    --cm-primary-hover: #0369a1;
    --cm-accent: #38bdf8;        /* Azzurro chiaro brillante */
    --cm-surface: #ffffff;
    --cm-border: #e2e8f0;
    --cm-text: #0f172a;
    --cm-text-muted: #64748b;
}

[data-bs-theme="dark"] {
    --cm-surface: #0f172a;       /* Sfondo scuro slate */
    --cm-border: #334155;
    --cm-text: #f8fafc;
    --cm-text-muted: #94a3b8;
}
```

### Gestione dei Temi (`ThemeToggle`)
L'applicazione supporta tre modalità: **Chiaro**, **Scuro** e **Sistema** (sincronizzato con le preferenze dell'OS tramite `window.matchMedia`).
Lo script `wwwroot/js/theme.js` sincronizza istantaneamente la preferenza tra `localStorage`, cookie per SSR e l'attributo `data-bs-theme` sull'elemento `<html>`.

---

## 2. Transizioni Fluide e Reattività a 60 FPS (ADR-038)

Per garantire un'esperienza d'uso pari a un'applicazione desktop nativa altamente reattiva:
1. **Zero-Lag sui Controlli**:
   - Feedback tattile immediato sui pulsanti (`:active { transform: scale(0.98); }`).
   - Barra di progresso superiore ultra-reattiva durante i cambi pagina Blazor (`.cm-page-loading-bar`).
2. **Animazioni Chiave Ottimizzate per GPU**:
   - Transizione morbida di ingresso pagina tramite animazione CSS `cm-page-enter` con proprietà `transform` e `opacity` accelerate via hardware.
   - Eliminazione del filtro `backdrop-filter: blur` su Linux WebKitGTK per prevenire stuttering del rendering software.

---

## 3. Componenti Condivisi Principali

| Componente | Scopo |
|---|---|
| `CardPreview` | Riquadro interattivo dell'anteprima live con debouncing (200 ms), gestione DPI e spinner di caricamento asincrono. |
| `AssetImage` | Rendering ottimizzato delle immagini da storage, che commuta automaticamente tra data URI (Desktop) ed endpoint HTTP (Web). |
| `FontPreviewImage` | Generazione a runtime dell'anteprima rasterizzata del font tipografico dato un testo campione. |
| `DynamicCardForm` | Form dinamico generato a partire dai metadati `FieldDefinition`, con supporto a campi condizionali e validazione client-side. |
| `ExportModal` | Finestra modale unificata per la selezione di formato (PNG, JPG, PDF), DPI (150, 300, 600), bleed e facciate. |
| `UserNavLinks` / `AdminNavLinks` | Barre di navigazione condivise per evitare discrepanze nei menu tra Desktop e Web. |
