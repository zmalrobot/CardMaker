# 01 — Anatomia delle carte (analisi di dominio)

Documento di riferimento per capire **quali elementi grafici il motore deve saper disegnare**.
La documentazione copre l'anatomia di dominio di **Yu-Gi-Oh!** (v1), **Pokémon TCG** (v2) e **Magic: The Gathering** (v2), tutte implementate e funzionanti nel motore data-driven.

---

## 1. Yu-Gi-Oh! — PRIORITÀ V1

**Formato fisico:** 59 × 86 mm (non è poker size). Raggio angoli ≈ 2 mm.
A 600 DPI → **1394 × 2031 px**. Con abbondanza 2 mm per lato → **1488 × 2125 px**.

### 1.1 Tipi di carta (ognuno = un template con frame diverso)

| Tipo | Frame | Particolarità |
|---|---|---|
| Monster Normale | giallo/beige | testo in *corsivo* (flavor text), niente effetto |
| Monster Effetto | arancione | |
| Monster Rituale | azzurro | |
| Monster Fusione | viola | |
| Monster Synchro | bianco | |
| Monster Xyz | nero | nome in **bianco**, stelle **Rank** allineate a **sinistra** |
| Monster Link | blu scuro | niente Livello, niente DEF, **8 frecce Link**, "LINK-n" in basso a destra |
| Token | grigio | |
| Magia (Spell) | verde | icona proprietà accanto a "[Magia]" |
| Trappola (Trap) | magenta | icona proprietà accanto a "[Trappola]" |
| Skill Card | azzurro (Speed Duel) | layout differente |
| Pendulum (× 6) | ibrido | metà alta = frame del tipo base, metà bassa verde, art **più largo**, box effetto Pendulum, due **Scale** |
| Dark Synchro | nero/viola | solo anime, livelli negativi |

**Totale template fronte classici: 17** (11 base + 6 Pendulum). A questi si aggiungono i template
Rush Duel (§ 1.6) e i 2 retro.

### 1.2 Specializzazioni (traits) — non cambiano il frame, cambiano la *type line*

`Spirit`, `Toon`, `Union`, `Gemini`, `Tuner`, `Flip`, `Pendulum`.

**Razze (25):** Dragon, Spellcaster, Warrior, Machine, Fiend, Zombie, Aqua, Beast, Beast-Warrior,
Winged Beast, Fairy, Insect, Dinosaur, Reptile, Fish, Sea Serpent, Plant, Pyro, Thunder, Rock,
Psychic, Wyrm, Cyberse, Illusion, Divine-Beast, Creator God.

La type line è **calcolata**: `[Razza / Metodo di evocazione / Abilità… / Effect]`
es. `[Dragon/Synchro/Tuner/Effect]`.

### 1.3 Simboli che l'admin deve caricare

| Set di simboli | Elementi |
|---|---|
| Attributi | LIGHT, DARK, EARTH, WATER, FIRE, WIND, DIVINE, SPELL, TRAP (9) |
| Proprietà Magia | Normal, Continuous, Equip, Quick-Play, Field, Ritual (6) |
| Proprietà Trappola | Normal, Continuous, Counter (3) |
| Stelle | Level (gialla), Rank (nera), Negative Level (3) |
| Frecce Link | 8 posizioni × stato on/off (16) |
| Ologramma | Eye of Anubis + varianti |
| Edizione | 1st Edition, Limited Edition, Duel Terminal |
| Rarità / foil | texture olografiche, trattamento del nome (argento / oro / secret) |

### 1.4 Campi della carta

`nome`, `attributo`, `livello|rank` (0–13), `linkRating` (calcolato dalle frecce), `frecceLink` (8 bool),
`scalePendulumSx`, `scalePendulumDx`, `razza`, `abilità` (multi), `metodoEvocazione`,
`testoEffetto` (rich text), `testoPendulum` (rich text), `flavorText` (corsivo),
`atk`, `def` (interi oppure `?`), `artwork`, `setId` (es. `LOB-IT001`), `passcode` (8 cifre),
`edizione`, `rarità`, `copyright`, `timbroOlografico`.

### 1.5 Peculiarità di rendering (critiche)

- **Il testo effetto è compresso orizzontalmente**, non solo rimpicciolito. Serve una scala X
  non uniforme sul font (in SkiaSharp: `SKFont.ScaleX`). Senza questo le carte non sembrano YGO.
- Le **stelle Livello** si riempiono da **destra verso sinistra**; le stelle **Rank** da sinistra verso destra.
- Il **nome** si comprime orizzontalmente per stare nella larghezza disponibile.
- I **Link Monster** non hanno DEF: al suo posto c'è `LINK-n`.
- Nei **Pendulum** l'artwork è più largo e il box testo è diviso in due (effetto Pendulum + effetto Monster).

### 1.6 Rush Duel — IN SCOPE V1

Formato parallelo (Yu-Gi-Oh! SEVENS / GO RUSH!!), stessa dimensione fisica **59 × 86 mm** ma con
identità grafica propria: artwork più grande, cornice più dinamica, retro differente.

**Tipi di carta:**

| Tipo | Note |
|---|---|
| Monster Normale | flavor text |
| Monster Effetto | box testo **strutturato in sezioni** |
| Monster Fusione | |
| **Maximum Monster** | carta divisa in **3 pezzi**: sinistro, centrale, destro |
| Magia | proprietà: Normal, Continuous, Equip, Quick, Field, Ritual |
| Trappola | proprietà: Normal, Continuous, Counter |
| Token | opzionale |

**Cosa NON esiste nel Rush Duel:** Xyz, Synchro, Link, Pendulum, Rank, Scale, frecce Link.
Restano: Attributo, Livello (stelle), Razza, ATK, DEF.

**Peculiarità rilevanti per il motore:**

1. **Box effetto a sezioni etichettate.** Il testo non è libero: è diviso in blocchi con intestazione
   — `[REQUISITO]`, `[EFFETTO]`, `[EFFETTO CONTINUO]`, `[EFFETTO A SCELTA MULTIPLA]`.
   Le etichette sono **generate dal motore** (quindi localizzabili), non disegnate nel frame.
   Si modellano come campi distinti (`requirementText`, `effectText`) resi da layer `richText` separati,
   oppure come un `repeatingBlock` di sezioni.
2. **Carte LEGEND.** Non sono un frame separato ma un **simbolo sovrapposto** (corona) attivato da un
   campo booleano → layer `staticImage` con `visibleWhen`.
3. **Maximum Monster.** Tre carte separate che compongono un'immagine unica se affiancate:
   - i pezzi **sinistro** e **destro** hanno ATK ma **non** DEF;
   - il pezzo **centrale** riporta il **MAXIMUM ATK**;
   - l'artwork è un terzo di un'illustrazione panoramica → lo slot immagine deve supportare un
     **crop orizzontale a fetta** (l'utente carica un'immagine larga e sceglie quale terzo usare).
4. **Rarità propria:** include l'**Over Rush Rare**, oltre alle rarità condivise con il formato classico.
5. **Retro dedicato** (`back-rush`).

> Impatto architetturale: Rush Duel **non richiede nuovi tipi di layer**. È la prima conferma pratica
> che il design data-driven regge — un intero formato aggiuntivo si esprime solo con dati.

---

## 2. Pokémon TCG — v2

**Formato fisico:** 63 × 88 mm (poker standard). A 600 DPI → **1488 × 2079 px**.

### 2.1 Tipi di carta

- **Pokémon**: Basic, Stage 1, Stage 2, Restored, V, VMAX, VSTAR, V-UNION, ex (SV), EX (vecchio),
  GX, Tag Team GX, LEGEND, Prime, Lv.X, BREAK, Prism Star, Radiant, Amazing Rare, Shining,
  Star (δ delta species), Baby, SP, Ultra Beast, Tera ex, Mega
- **Trainer**: Item, Supporter, Stadium, Pokémon Tool, Tool F, Technical Machine, ACE SPEC,
  Rocket's Secret Machine
- **Energy**: Basic Energy (9 tipi), Special Energy

**Meccaniche trasversali:** Battle Styles (Single / Rapid / Fusion Strike), Ancient / Future,
Forme regionali (Alolan, Galarian, Hisuian, Paldean).

### 2.2 Simboli

Tipi energia (11: Grass, Fire, Water, Lightning, Psychic, Fighting, Darkness, Metal, Fairy, Dragon,
Colorless), simboli espansione (uno per set), simboli rarità (●, ◆, ★, …), regulation mark (lettera in box),
barre di intestazione Ability / Poké-POWER / Poké-BODY / VSTAR Power, icone Weakness / Resistance / Retreat.

### 2.3 Campi

Stage + "Evolves from X" (con **bolla immagine** del pre-evoluzione), nome, HP, tipo,
Ability (nome + testo), **lista attacchi variabile (1–3)** ognuno con costo energia (icone), nome,
danno (`120`, `120+`, `120×`, `20-`) e testo; Weakness (tipo + moltiplicatore), Resistance,
Retreat cost (N icone), rule box, dati Pokédex (categoria, altezza, peso, numero), flavor text,
illustratore, numero collezione `25/102`, copyright.

### 2.4 Peculiarità

**Layout a flusso verticale**: il numero di attacchi e la lunghezza dei testi ridistribuiscono lo spazio
del box. Non basta il posizionamento assoluto — serve un mini layout engine tipo flexbox
(`repeatingBlock` ad altezza variabile). Esistono inoltre carte **full-art** e **orizzontali**.

---

## 3. Magic: The Gathering — v2

**Formato fisico:** 63 × 88 mm, raggio angoli 3.18 mm (0.125").

### 3.1 Tipi e frame

**Tipi:** Artifact, Battle, Creature, Enchantment, Instant, Land, Planeswalker, Sorcery, Kindred.
**Supertipi:** Basic, Legendary, Snow, World, Ongoing.
**Colori/frame:** White, Blue, Black, Red, Green, Multicolor (oro), Hybrid (gradiente a 2 colori),
Artifact, Colorless/Devoid, Land, Nyx, Vehicle, Snow, Token, Emblem.

**Layout (il vero problema):** Normal, Split, Aftermath, Flip, Transform DFC, Modal DFC, Meld,
Adventure, Leveler, Saga, Class, Room, Case, Planeswalker (2–4 abilità loyalty), Battle (orizzontale),
Prototype, Mutate, Vanguard, Plane/Phenomenon, Scheme, Conspiracy, Attraction, Sticker.

**Ere di frame:** Original (1993–2003), Modern/8th (2003–2014), M15 (2014–oggi),
più Showcase / Borderless / Extended Art / Retro / Textless.

### 3.2 Simboli

Mana: `{W}{U}{B}{R}{G}{C}`, generici `{0}`–`{20}`, `{X}{Y}{Z}`, ibridi (`{W/U}`…), ibridi monocolore
(`{2/W}`…), Phyrexian (`{W/P}`…), `{S}` neve, `{T}` tap, `{Q}` untap, `{E}` energy.
Più: simboli espansione (× 4-5 colori di rarità), watermark di gilda/fazione, holofoil stamp,
icone loyalty (+/−/0), defense box, capitoli Saga (I, II, III, IV), color indicator.

### 3.3 Campi

Nome, costo di mana, artwork, type line (`Legendary Creature — Human Wizard`), simbolo espansione,
rules text (con simboli inline), flavor text (corsivo, separato da barra), P/T o Loyalty o Defense,
artista, collector number `0123/291`, set code, lingua, rarità (C/U/R/M), copyright, watermark.

### 3.4 Peculiarità

Simboli **inline** nel flusso del testo allineati alla baseline; auto-fit con riduzione simultanea di
corpo font e interlinea; frame **hybrid** con gradiente fra due colori.

---

## 4. Tipografia: un ruolo font per ogni elemento di testo

> **Requisito trasversale.** Ogni singolo elemento testuale di una carta deve avere il **proprio font
> caricabile dall'admin** e il proprio stile completamente configurabile. Non esiste un "font della
> carta": esiste un font del *nome*, uno del *testo effetto*, uno dei *numeri ATK*, e cosi' via.

### 4.1 Cosa deve essere configurabile su ogni testo

| Proprieta' | Note |
|---|---|
| Font | riferimento a un font caricato dall'admin, tramite **alias di ruolo** |
| Corpo (pt) | con minimo e massimo per l'auto-fit |
| Colore | tinta piatta, gradiente o texture (serve per il nome oro/argento delle rarita') |
| Contorno (stroke) | colore e spessore |
| Ombra | scostamento, sfocatura, colore |
| Spaziatura | fra lettere, fra parole, interlinea |
| **Scala orizzontale** | compressione non uniforme: indispensabile per Yu-Gi-Oh! |
| Allineamento | orizzontale e verticale |
| Trasformazione | nessuna / MAIUSCOLO / minuscolo / Iniziali Maiuscole |
| Auto-fit | strategia, corpo minimo, compressione minima, interlinea minima |
| Padding | margine interno alla casella |

### 4.2 Ruoli font — Yu-Gi-Oh! classico

| Elemento della carta | Alias del ruolo | Caratteristiche |
|---|---|---|
| Nome carta | `card-name` | serif marcato, regge bene la compressione orizzontale |
| Etichetta `[Magia Continua]` / `[Trappola]` | `spell-trap-label` | bold, corpo piccolo |
| Type line `[Dragon/Synchro/Tuner/Effect]` | `type-line` | bold, corpo piccolo, molto leggibile |
| Testo effetto | `effect` | regular |
| Flavor text dei Monster Normali | `effect-italic` | **corsivo** |
| Testo Pendulum | `pendulum-effect` | spesso identico a `effect` |
| Numeri Scale Pendulum | `pendulum-scale` | cifre grandi e leggibili |
| Etichette "ATK" / "DEF" | `atk-def-label` | |
| Valori ATK / DEF | `atk-def-value` | **cifre tabulari**, condensato |
| Valore `LINK-n` | `link-rating` | |
| Codice set (`LOB-IT001`) | `set-code` | sans, corpo minuscolo |
| Edizione (`1st Edition`) | `edition` | se resa come testo invece che come simbolo |
| Passcode a 8 cifre | `passcode` | sans, corpo minuscolo |
| Copyright | `copyright` | sans, corpo minuscolo |

### 4.3 Ruoli font — Rush Duel

| Elemento | Alias | Note |
|---|---|---|
| Nome carta Rush | `rush-card-name` | solo se graficamente diverso dal classico |
| Etichette `[REQUISITO]` / `[EFFETTO]` | `rush-section-label` | bold, spesso su fondo colorato |
| Testo requisito ed effetto | `rush-effect` | |
| Valore `MAXIMUM ATK` | `rush-maximum-atk` | cifre molto grandi |
| Type line Rush | `rush-type-line` | |

### 4.4 Ruoli font — Pokémon (v2)

`pkm-card-name`, `pkm-hp-label`, `pkm-hp-value`, `pkm-stage`, `pkm-evolves-from`,
`pkm-ability-header`, `pkm-ability-name`, `pkm-attack-name`, `pkm-attack-damage`,
`pkm-attack-text`, `pkm-rule-box`, `pkm-flavor`, `pkm-pokedex-data`, `pkm-illustrator`,
`pkm-collector-number`, `pkm-weakness-resistance`.

### 4.5 Ruoli font — Magic (v2)

`mtg-card-name`, `mtg-type-line`, `mtg-rules-text`, `mtg-flavor-text` (corsivo),
`mtg-power-toughness`, `mtg-loyalty`, `mtg-artist`, `mtg-collector-number`, `mtg-set-code`,
`mtg-copyright`.

### 4.6 Come funziona nel programma

1. L'admin **carica un file font** (`.ttf` / `.otf` / `.woff2`) e gli assegna un **alias di ruolo**.
2. Il motore, quando incontra `"font": "card-name"` in un layout, risolve l'alias sul font del gioco.
3. Cambiare il font di *tutti i nomi carta* significa ricaricare un solo file: nessun template va toccato.
4. Se un alias non e' assegnato, il motore usa un **font di fallback libero** e segnala l'anomalia:
   non fallisce il render, ma l'anteprima avvisa che manca un font.

---

## 5. Sintesi: capacita' richieste al motore

| Capacità | YGO | PKM | MTG |
|---|:--:|:--:|:--:|
| Immagini statiche sovrapposte (frame, overlay) | ✅ | ✅ | ✅ |
| Slot immagine utente con crop/zoom/maschera | ✅ | ✅ | ✅ |
| Crop "a fetta" per immagini panoramiche (Maximum Rush) | ✅ | – | – |
| Testo con auto-fit (shrink) | ✅ | ✅ | ✅ |
| Testo con **compressione orizzontale** | ✅ | – | – |
| Rich text con **simboli inline** | – | ✅ | ✅ |
| Ripetitore di simboli (stelle, costi energia, retreat) | ✅ | ✅ | – |
| Gruppo di toggle (frecce Link) | ✅ | – | ✅ (Saga) |
| Blocchi ripetuti ad **altezza variabile** | – | ✅ | ✅ |
| Layer condizionali (regole) | ✅ | ✅ | ✅ |
| Overlay foil con blend mode | ✅ | ✅ | ✅ |
| Testo calcolato/derivato (type line) | ✅ | – | ✅ |
| **Font distinto per ogni elemento di testo** | ✅ | ✅ | ✅ |
| Gradienti (frame hybrid) | – | – | ✅ |
| Orientamento orizzontale | – | ✅ | ✅ |
| Doppia faccia / transform | ✅ (retro) | ✅ (retro) | ✅ (DFC) |
