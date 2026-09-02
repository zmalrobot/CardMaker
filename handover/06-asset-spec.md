# 06 — Specifica degli asset grafici

Documento **da consegnare a chi realizza la grafica**. Definisce formati, dimensioni, convenzioni di
nomenclatura e l'elenco completo dei file necessari per il gioco Yu-Gi-Oh! (formato classico + Rush Duel).

> Le chiavi (`key`) usate qui corrispondono **esattamente** alle chiavi del database
> (`SymbolSet.Key`, `Symbol.Key`, `Template.Key`) descritte in [`03-data-model.md`](03-data-model.md).
> Rispettarle evita lavoro di rimappatura.

---

## 1. Regole tecniche generali

| Regola | Valore |
|---|---|
| Formato file | **PNG-24 con canale alpha** (PNG-32). No JPG, no **SVG** (bloccato per sicurezza), no WEBP per i frame |
| Spazio colore | **sRGB**, 8 bit per canale. Nessun profilo ICC incorporato diverso da sRGB |
| Stampa | **Solo RGB.** Nessun CMYK: non è prevista stampa professionale |
| Interlacing | Disattivato |
| Metadati | Rimuovere EXIF e dati personali (il motore ri-codifica comunque i file in upload) |
| Peso massimo per file | 10 MB |
| Trasparenza | **Obbligatoria** dove indicato. Mai fondo bianco al posto della trasparenza |
| Nomenclatura | minuscolo, parole separate da `-`, niente spazi/accenti/maiuscole |

---

## 2. Il canvas della carta

Formato fisico Yu-Gi-Oh!: **59 × 86 mm**. Risoluzione di lavoro: **600 DPI**.

```
┌──────────────────────────────────────────┐  ← MASTER CANVAS 1488 × 2125 px  (63 × 89,96 mm)
│  area di abbondanza (bleed) 47 px = 2 mm │
│  ┌────────────────────────────────────┐  │  ← TRIM  1394 × 2031 px  (59 × 86 mm)
│  │                                    │  │     origine (47, 47), raggio angoli 47 px
│  │   ┌────────────────────────────┐   │  │
│  │   │                            │   │  │  ← SAFE ZONE 1252 × 1889 px
│  │   │   niente di importante     │   │  │     origine (118, 118) — margine 3 mm dal trim
│  │   │   fuori da quest'area      │   │  │
│  │   └────────────────────────────┘   │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
```

| Misura | px @600 DPI | mm |
|---|---|---|
| Master canvas (con abbondanza) | **1488 × 2125** | 63 × 89,96 |
| Trim (bordo di taglio) | **1394 × 2031** | 59 × 86 |
| Offset del trim nel master | **47, 47** | 2, 2 |
| Raggio angoli | **47** | 2 |
| Safe zone | **1252 × 1889** @ (118, 118) | margine 3 mm |

> Misure verificate da un test automatico (`CardGeometryTests`). L'invariante che regge tutto è
> **master = trim + 2 × abbondanza**: non arrotondare le due misure in modo indipendente.

**Tutti i frame e tutte le texture a piena carta vanno consegnati a 1488 × 2125 px.**
La grafica deve estendersi fino ai bordi del master (nell'area di abbondanza), così l'export senza
abbondanza può semplicemente ritagliare senza lasciare bordi bianchi.

Gli angoli arrotondati **non vanno disegnati**: li applica il motore con una maschera.
Consegnare quindi il frame con gli angoli pieni, rettangolari.

---

## 3. Come va costruito un frame

Il frame è un **unico PNG sovrapposto all'artwork**. Il motore disegna in quest'ordine:

```
1. artwork dell'utente   (riempie la finestra)
2. frame.png             (sopra, con la finestra TRASPARENTE)
3. testi e simboli       (sopra ancora)
4. overlay foil          (eventuale, in blend mode)
```

### Requisiti obbligatori del frame

1. La **finestra dell'artwork deve essere completamente trasparente** (alpha = 0). L'artwork viene
   disegnato *sotto* il frame e traspare da lì.
2. **Nessun testo** disegnato nel frame: né il nome, né "ATK/DEF", né "[Magia]", né il copyright.
   Tutti i testi sono layer del motore, così sono localizzabili e modificabili.
3. **Nessun simbolo** disegnato nel frame: attributi, stelle, ologrammi sono file separati.
4. Le **caselle di testo** (box effetto, type line) fanno parte del frame come grafica di sfondo,
   ma restano vuote.

### Metadati di accompagnamento (molto utili)

Per ogni frame, consegnare accanto un file `<nome>.meta.json` con i rettangoli chiave in pixel
sul master canvas. Fa risparmiare ore di posizionamento manuale nell'editor:

```json
{
  "frame": "monster-effect",
  "canvas": { "w": 1488, "h": 2125 },
  "regions": {
    "artWindow":    { "x": 178, "y": 350, "w": 1132, "h": 1132 },
    "nameBox":      { "x": 130, "y": 120, "w": 1050, "h": 130  },
    "attributeBox": { "x": 1210, "y": 120, "w": 160, "h": 160  },
    "levelStrip":   { "x": 200, "y": 265, "w": 1110, "h": 90   },
    "typeLineBox":  { "x": 150, "y": 1510, "w": 1190, "h": 70  },
    "effectBox":    { "x": 150, "y": 1590, "w": 1190, "h": 330 },
    "atkBox":       { "x": 780, "y": 1930, "w": 250, "h": 70   },
    "defBox":       { "x": 1060, "y": 1930, "w": 250, "h": 70  },
    "setIdBox":     { "x": 1050, "y": 300, "w": 260, "h": 50   },
    "hologramBox":  { "x": 1290, "y": 1955, "w": 80, "h": 80   }
  }
}
```

*(i valori sopra sono un esempio illustrativo, non misure ufficiali)*

---

## 4. Elenco dei frame — Yu-Gi-Oh! classico

Cartella: `ygo/frames/`

| # | Chiave file | Descrizione | Note |
|---|---|---|---|
| 1 | `monster-normal.png` | Monster Normale | frame giallo/beige |
| 2 | `monster-effect.png` | Monster Effetto | arancione |
| 3 | `monster-ritual.png` | Monster Rituale | azzurro |
| 4 | `monster-fusion.png` | Monster Fusione | viola |
| 5 | `monster-synchro.png` | Monster Synchro | bianco |
| 6 | `monster-xyz.png` | Monster Xyz | nero |
| 7 | `monster-link.png` | Monster Link | blu scuro, **niente casella DEF** |
| 8 | `token.png` | Token | grigio |
| 9 | `spell.png` | Magia | verde |
| 10 | `trap.png` | Trappola | magenta |
| 11 | `skill.png` | Skill Card | Speed Duel, layout proprio |
| 12 | `pendulum-normal.png` | Pendulum Normale | art **più largo**, doppio box testo |
| 13 | `pendulum-effect.png` | Pendulum Effetto | |
| 14 | `pendulum-ritual.png` | Pendulum Rituale | |
| 15 | `pendulum-fusion.png` | Pendulum Fusione | |
| 16 | `pendulum-synchro.png` | Pendulum Synchro | |
| 17 | `pendulum-xyz.png` | Pendulum Xyz | |

**Opzionali (bassa priorità):** `dark-synchro.png`.

---

## 5. Elenco dei frame — Rush Duel

Cartella: `ygo/frames/rush/`

| # | Chiave file | Descrizione | Note |
|---|---|---|---|
| 1 | `rush-monster-normal.png` | Monster Normale Rush | |
| 2 | `rush-monster-effect.png` | Monster Effetto Rush | box testo diviso in **[REQUISITO]** e **[EFFETTO]** |
| 3 | `rush-monster-fusion.png` | Monster Fusione Rush | |
| 4 | `rush-maximum-left.png` | Maximum — pezzo sinistro | ha **ATK** ma non DEF |
| 5 | `rush-maximum-center.png` | Maximum — pezzo centrale (Maximum Mode) | ha **MAXIMUM ATK** |
| 6 | `rush-maximum-right.png` | Maximum — pezzo destro | ha **ATK** ma non DEF |
| 7 | `rush-spell.png` | Magia Rush | |
| 8 | `rush-trap.png` | Trappola Rush | |
| 9 | `rush-token.png` | Token Rush | *opzionale* |

### Note specifiche Rush Duel

- L'artwork nei Rush Duel è **più grande** rispetto al formato classico: prevedere una finestra più ampia.
- Le carte **LEGEND** non sono un frame separato: sono un **simbolo sovrapposto** (`rush-legend`)
  su un frame normale.
- Il box effetto è **strutturato in sezioni etichettate** (`[REQUISITO]`, `[EFFETTO]`,
  `[EFFETTO CONTINUO]`, `[EFFETTO A SCELTA MULTIPLA]`): le etichette sono **testo generato dal motore**,
  non grafica del frame. Il frame deve solo prevedere lo spazio.
- Rush Duel **non ha** Xyz, Synchro, Link, Pendulum, Rank né Scale.

---

## 6. Retro delle carte

Cartella: `ygo/backs/` · dimensione **1488 × 2125 px**, nessuna trasparenza richiesta.

| Chiave file | Descrizione |
|---|---|
| `back-classic.png` | Retro standard OCG/TCG |
| `back-rush.png` | Retro Rush Duel |

---

## 7. Simboli

Tutti i simboli: **PNG con fondo trasparente**, **quadrati**, soggetto centrato con circa il 5% di
margine trasparente su ogni lato. Vanno consegnati a una risoluzione **maggiore o uguale** a quella
d'uso: il motore può solo rimpicciolire, mai ingrandire senza perdita.

### 7.1 `attributes` — `ygo/symbols/attributes/` — **512 × 512 px**

`light` · `dark` · `earth` · `water` · `fire` · `wind` · `divine` · `spell` · `trap`  → **9 file**

### 7.2 `spell-properties` — `ygo/symbols/spell-properties/` — **256 × 256 px**

`normal` · `continuous` · `equip` · `quick-play` · `field` · `ritual`  → **6 file**

> `normal` può essere un PNG completamente trasparente (sulle Magie Normali non appare icona),
> ma va comunque consegnato per uniformità.

### 7.3 `trap-properties` — `ygo/symbols/trap-properties/` — **256 × 256 px**

`normal` · `continuous` · `counter`  → **3 file**

### 7.4 `level-stars` — `ygo/symbols/level-stars/` — **256 × 256 px**

| Chiave | Uso |
|---|---|
| `level` | stella Livello (dorata) — si riempie **da destra verso sinistra**, max 12 |
| `rank` | stella Rank (nera) — si riempie **da sinistra verso destra**, max 13 |
| `negative-level` | livello negativo (Dark Synchro) — *opzionale* |
| `rush-level` | stella Livello Rush Duel — se graficamente diversa |

→ **3–4 file**

### 7.5 `link-arrows` — `ygo/symbols/link-arrows/` — **512 × 512 px** ciascuna

Otto posizioni, ognuna in **due stati**. Le frecce devono essere disegnate **nella loro orientazione
finale** (non ruotate dal motore) perché su carta hanno forme leggermente diverse.

| Posizione | File acceso | File spento |
|---|---|---|
| Alto-sinistra | `top-left-on.png` | `top-left-off.png` |
| Alto | `top-on.png` | `top-off.png` |
| Alto-destra | `top-right-on.png` | `top-right-off.png` |
| Sinistra | `left-on.png` | `left-off.png` |
| Destra | `right-on.png` | `right-off.png` |
| Basso-sinistra | `bottom-left-on.png` | `bottom-left-off.png` |
| Basso | `bottom-on.png` | `bottom-off.png` |
| Basso-destra | `bottom-right-on.png` | `bottom-right-off.png` |

→ **16 file**

### 7.6 `holograms` — `ygo/symbols/holograms/` — **256 × 256 px**

`anubis-silver` · `anubis-gold` · `rush` *(opzionale)*  → **2–3 file**

### 7.7 `editions` — `ygo/symbols/editions/` — **1024 × 256 px** (orizzontali)

`first-edition` · `limited-edition` · `duel-terminal`  → **3 file**

> Se preferite gestirle come testo tipografico invece che come immagine, segnalatelo: si possono
> realizzare come layer di testo.

### 7.8 `rush-markers` — `ygo/symbols/rush-markers/` — **512 × 512 px**

`legend` (corona LEGEND) · `maximum` (indicatore Maximum) · `rush-logo`  → **3 file**

---

## 8. Rarità e foil

Cartella: `ygo/foils/` · dimensione **1488 × 2125 px** (piena carta), PNG con alpha.

Le texture olografiche vengono applicate dal motore con un **blend mode** (`screen`, `overlay`,
`softLight`) e un'opacità regolabile, eventualmente limitate a una zona tramite maschera.
Vanno quindi disegnate come **texture neutre su fondo trasparente o nero**, non come carte finite.

| Chiave | Descrizione | Applicata a |
|---|---|---|
| `common` | nessuna texture (file non necessario) | — |
| `rare` | leggero luccichio | nome |
| `super-rare` | olografia sull'artwork | solo artwork |
| `ultra-rare` | olografia sull'artwork + nome dorato | artwork |
| `secret-rare` | texture a righe diagonali arcobaleno | tutta la carta |
| `ultimate-rare` | rilievo/embossing | tutta la carta |
| `ghost-rare` | effetto 3D lattiginoso | tutta la carta |
| `starlight-rare` | reticolo a diamanti | tutta la carta |
| `collectors-rare` | texture incisa | tutta la carta |
| `quarter-century` | texture Quarter Century | tutta la carta |
| `over-rush-rare` | rarità Rush Duel | tutta la carta |

→ **~10 texture**

### Maschere di applicazione — `ygo/masks/` — **1488 × 2125 px**

PNG in scala di grigi (bianco = applica, nero = non applicare):

`mask-art-standard` · `mask-art-pendulum` · `mask-art-rush` · `mask-art-maximum` · `mask-full-card`

→ **5 file**

### Trattamento del nome per rarità

Non serve un'immagine per ogni rarità. Consegnare:
- i **colori esadecimali** per nome nero, argento e dorato;
- una **texture tileable 512 × 128 px** per il nome "secret" (arcobaleno) e per l'oro, se si vuole
  un riempimento a texture invece che a tinta piatta.

---

## 9. Font

I font **non sono inclusi nell'applicazione**: li carica l'amministratore. Consegnare file `.ttf` o
`.otf` (`.woff2` accettato) **legalmente utilizzabili e incorporabili**, indicando per ognuno la licenza.

### 9.1 Principio: un ruolo per ogni elemento di testo

Non esiste un "font della carta". Ogni elemento testuale ha il **proprio ruolo**, identificato da un
**alias**: il nome, il testo effetto, i numeri ATK, il codice set sono font distinti e sostituibili
in modo indipendente. Cambiare il font di tutti i nomi carta significa ricaricare **un solo file**,
senza toccare alcun template.

Un file font puo' coprire piu' ruoli: se il testo effetto e la type line usano lo stesso font,
si carica una volta sola e gli si assegnano due alias.

### 9.2 Ruoli richiesti — Yu-Gi-Oh! classico

| Alias | Elemento | Caratteristiche |
|---|---|---|
| `card-name` | Nome della carta | Serif marcato, deve reggere la **compressione orizzontale** |
| `spell-trap-label` | `[Magia Continua]`, `[Trappola]` | Bold, corpo piccolo |
| `type-line` | `[Dragon/Synchro/Tuner/Effect]` | Bold, corpo piccolo, molto leggibile |
| `effect` | Testo effetto | Regular |
| `effect-italic` | Flavor text dei Monster Normali | **Corsivo** |
| `effect-bold` | Enfasi dentro il testo effetto | Bold *(opzionale)* |
| `pendulum-effect` | Testo Pendulum | Spesso identico a `effect` |
| `pendulum-scale` | Numeri Scale Pendulum | Cifre grandi e leggibili |
| `atk-def-label` | Etichette "ATK" / "DEF" | |
| `atk-def-value` | Valori ATK / DEF | Condensato, **cifre tabulari** |
| `link-rating` | Valore `LINK-n` | |
| `set-code` | Codice set (`LOB-IT001`) | Sans, corpo minuscolo |
| `edition` | `1st Edition` | Solo se resa come testo |
| `passcode` | Passcode a 8 cifre | Sans, corpo minuscolo |
| `copyright` | Riga di copyright | Sans, corpo minuscolo |

### 9.3 Ruoli richiesti — Rush Duel

| Alias | Elemento |
|---|---|
| `rush-card-name` | Nome carta Rush |
| `rush-section-label` | Etichette `[REQUISITO]` / `[EFFETTO]` |
| `rush-effect` | Testo di requisito ed effetto |
| `rush-type-line` | Type line Rush |
| `rush-maximum-atk` | Valore `MAXIMUM ATK`, cifre molto grandi |

> Se un ruolo Rush coincide graficamente con quello classico, **non serve un file in piu'**:
> basta assegnare il secondo alias allo stesso font.

### 9.4 Requisiti tecnici dei file font

| Requisito | Dettaglio |
|---|---|
| Formati | `.ttf`, `.otf`, `.woff2` |
| Set di caratteri | **Latin-1 completo**. Accentate italiane obbligatorie: à è é ì ò ù + maiuscole |
| Cifre | **Tabulari** (larghezza fissa) almeno per `atk-def-value` e `pendulum-scale` |
| Licenza | Deve consentire l'**embedding nel PDF** esportato |
| Hinting | Consigliato, migliora la resa dell'anteprima a bassa risoluzione |
| Peso massimo | 8 MB per file |

### 9.5 Priorità

Per sbloccare lo sviluppo bastano **4 font**: `card-name`, `effect`, `effect-italic`, `atk-def-value`.
Tutti gli altri alias possono puntare temporaneamente a questi quattro.

---

## 10. Riepilogo della consegna

```
ygo/
├─ frames/
│  ├─ monster-normal.png            + monster-normal.meta.json
│  ├─ …                                (17 frame classici)
│  └─ rush/
│     ├─ rush-monster-normal.png    + .meta.json
│     └─ …                             (8–9 frame Rush)
├─ backs/
│  ├─ back-classic.png
│  └─ back-rush.png
├─ symbols/
│  ├─ attributes/          (9)
│  ├─ spell-properties/    (6)
│  ├─ trap-properties/     (3)
│  ├─ level-stars/         (3–4)
│  ├─ link-arrows/        (16)
│  ├─ holograms/           (2–3)
│  ├─ editions/            (3)
│  └─ rush-markers/        (3)
├─ foils/                 (~10)
├─ masks/                   (5)
├─ fonts/                 (6–8 famiglie)
└─ LICENSES.md            ← provenienza e licenza di OGNI asset e font
```

**Totale indicativo: ~100 file.**

### Ordine di priorità per lo sviluppo

Non serve tutto subito. Per sbloccare le fasi F3-F5 basta il **pacchetto minimo**:

1. `monster-effect.png` + `.meta.json`
2. `spell.png`, `trap.png`
3. I 9 simboli `attributes`
4. `level-stars/level.png`
5. I 6 simboli `spell-properties` e i 3 `trap-properties`
6. `back-classic.png`
7. I font `card-name`, `effect` (regular + italic), `type-line`, `atk-def`

Con questi si costruisce e si valida l'intera catena end-to-end; il resto si aggiunge in modo incrementale.

---

## 11. Nota legale

`LICENSES.md` deve documentare, per ogni asset e font, **origine e diritto d'uso**. È il presidio che
giustifica il principio di progetto per cui l'applicazione non distribuisce alcun materiale protetto
(vedi [ADR-010](05-decisions.md#adr-010--nessun-asset-grafico-distribuito-con-lapplicazione)).
Il campo licenza è obbligatorio anche nel database, su ogni asset caricato.
