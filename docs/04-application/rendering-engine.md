# Motore di Rendering Tipografico (SkiaSharp)

Il motore di rendering (`CardMaker.Rendering`) è il componente centrale del sistema ed è responsabile della rasterizzazione grafica e della composizione vettoriale delle carte.

---

## 1. Architettura a Strategy Painters

In seguito al refactoring architetturale (ottimizzazione `REF-001`), la classe `CardRenderer` non contiene più logica monolitica di disegno, ma orchestra una suite di **Strategy Painters** specializzati che implementano l'interfaccia interna `ILayerPainter`:

```mermaid
graph TD
    CR[CardRenderer] --> ILP[ILayerPainter Interface]
    ILP --> IP[ImageLayerPainter<br/>Frame e Artwork con crop]
    ILP --> TP[TextLayerPainter<br/>Testi con auto-fit e CapHeight]
    ILP --> RP[RichTextLayerPainter<br/>Embedding glifi inline {sym:...}]
    ILP --> SP[SymbolRepeaterLayerPainter<br/>Stelle, livelli e rank]
    ILP --> LP[LinkArrowsLayerPainter<br/>Frecce Link ottagonali]
    ILP --> FP[FoilLayerPainter<br/>Texture olografiche e maschere]
    CR --> RPP[RenderPostProcessor<br/>Angoli arrotondati, bleed e safe zone]
```

### I Painter nel Dettaglio
- **`ImageLayerPainter`**: Gestisce il disegno di immagini statiche (frame, icone fisse) e artwork caricati dall'utente. Supporta il ritaglio geometrico proporzionale (*crop/fill*) per adattare qualsiasi proporzione d'immagine alla finestra del template.
- **`TextLayerPainter`**: Gestisce testi a singola o multipla riga con allineamento tipografico, rotazione, ombra e centraggio ottico calibrato sull'altezza delle maiuscole (`CapHeight`) anziché sul box ascesa/discesa.
- **`RichTextLayerPainter`**: Analizza il testo alla ricerca di token `{sym:set-key.symbol-key}`, risolve i glifi vettoriali tramite `FontRegistry` e intercala icone e testo calcolando l'avanzamento ottico in tempo reale.
- **`SymbolRepeaterLayerPainter`**: Calcola la disposizione di N simboli ripetuti (es. le 12 stelle di Yu-Gi-Oh!) orizzontalmente o verticalmente, con spaziatura fissa o dinamica.
- **`LinkArrowsLayerPainter`**: Disegna le 8 frecce direzionali dei mostri Link, illuminando con colore attivo solo quelle specificate nella carta.
- **`FoilLayerPainter`**: Applica texture metallizzate o arcobaleno con modalità di fusione SkiaSharp (`SKBlendMode.Overlay`, `SKBlendMode.Screen`) per simulare carte olografiche e rare.

---

## 2. TextEngine e Algoritmi di Auto-Fit

Il motore tipografico `TextEngine` implementa tre strategie di adattamento automatico del testo quando la stringa eccede i confini del rettangolo assegnato:

1. **`shrink`**: Riduce progressivamente la dimensione del font in punti (`TextSize`) fino a raggiungere la dimensione minima consentita (`MinFontSize`).
2. **`condense`**: Mantiene inalterata la dimensione del font e comprime l'asse orizzontale tramite la proprietà nativa `SKFont.ScaleX` (indispensabile per riprodurre fedelmente l'aspetto tipografico dei titoli e delle caselle di Yu-Gi-Oh!).
3. **`shrink-and-condense`**: Strategia ibrida calibrata: prima applica una compressione orizzontale moderata, poi riduce gradualmente la dimensione in punti se il testo continua a eccedere i limiti.

---

## 3. Generatori di Simboli Procedurali

Per garantire il collaudo immediato e l'indipendenza da asset protetti da copyright, `CardMaker.Rendering` include generatori geometrici SkiaSharp procedurali:
- **Energie Pokémon**: Genera proceduralmente su tela vettoriale i simboli per Erba, Fuoco, Acqua, Lampo, Psico, Lotta, Oscurità, Metallo e Incolore.
- **Mana e Rarità Magic**: Genera le sfere di mana ({W}, {U}, {B}, {R}, {G}, {C}, numeri generici, {X}, {T}) e gli elisir di rarità (Comune, Non comune, Rara, Mitica).
- **Segnaposto Frame**: Produce cornici complete con bordi, finestre d'illustrazione e caselle di testo per tutti i tipi di carta.

---

## 4. Esportatore Vettoriale PDF (`PdfExporter`)

La generazione di documenti PDF avviene tramite `SKDocument.CreatePdf` e utilizza gli **stessi identici comandi di disegno** della rasterizzazione su `SKSurface`:
- Testi vettoriali nitidi e curve matematiche senza perdita di risoluzione.
- Supporto al fronte singolo o all'unione automatica di fronte e retro in un documento a due pagine.
- Dimensionamento conforme agli standard di stampa internazionali con abbondanza (*Bleed*) inclusa o ritagliata a filo (*Trim*).
