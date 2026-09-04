# Gestione Contenuti e Template Studio

Gli strumenti dedicati agli amministratori consentono la configurazione completa del dominio, degli asset e dei template visuali.

---

## 1. Gestione Contenuti Modulare (`ContentManager`)

In seguito al refactoring architetturale (ottimizzazione `REF-002`), la pagina di amministrazione contenuti (`/admin/content`) è organizzata in componenti tabulari dedicati situati in `src/CardMaker.UI/Pages/Admin/ContentTabs/`:

1. **`GamesTab`**: Gestione dei giochi supportati, con configurazione delle specifiche fisiche (larghezza, altezza in millimetri, raggio angoli, abbondanza e safe zone).
2. **`CardTypesTab`**: Definizione delle tipologie di carta per ciascun gioco (es. Mostro Rituale, Fase 2, Planeswalker), abilitazione dei tratti e collegamento al template grafico predefinito.
3. **`TraitsTab`**: Gestione delle specializzazioni trasversali (es. Tuner, Toon, Spirit, Mega, GX, Leggendaria).
4. **`SymbolSetsTab`**: Definizione delle famiglie di simboli e upload dei singoli glifi grafici.
5. **`OptionListsTab`**: Gestione dei menu a tendina riutilizzabili dagli utenti (attributi, elementi, rarità, tipi mostro).

---

## 2. Editor dello Schema Campi (`SchemaEditor`)

Raggiungibile all'indirizzo `/admin/schema/{id}`, consente di definire quali campi l'utente compilerà durante la creazione della carta per uno specifico `CardType`:
- **Proprietà del campo**: Chiave univoca (`Key`), etichetta visuale (`Label`), tipo di dato (`Text`, `MultilineText`, `Number`, `Dropdown`, `Image`, `Boolean`, `LinkArrows`).
- **Validazione**: Valore predefinito, obbligatorietà, lunghezza minima/massima, regex di formato.
- **Dipendenze Condizionali**: Definizione di regole per mostrare o nascondere un campo in base alla selezione di un altro valore (es. mostrare il campo "Frecce Link" solo se il tipo è "Monster Link").
- **Anteprima Live**: La pagina include un simulatore interattivo del form che reagisce in tempo reale alle modifiche dello schema.

---

## 3. Template Studio WYSIWYG (`TemplateEditor`)

Raggiungibile all'indirizzo `/admin/templates/{id}`, è l'ambiente grafico avanzato a 3 pannelli per la composizione dei layout di stampa:

```text
┌─────────────────────────┬─────────────────────────────────┬─────────────────────────┐
│     PANNELLO LAYER      │        VIEWPORT CENTRALE        │   ISPETTORE PROPRIETÀ   │
│                         │                                 │                         │
│ - Albero dei layer con  │ - Zoom interattivo (25% - 400%) │ - Coordinate (X, Y,     │
│   ordinamento Z-order   │ - Guida Trim Box (linea taglio) │   W, H normalizzate)    │
│ - Drag & Drop riordino  │ - Guida Bleed Box (+2 mm rosso) │ - Selezione font e alias│
│ - Aggiunta layer rapidi │ - Guida Safe Zone (3 mm verde)  │ - Auto-fit tipografico  │
│   (Image, Text, RichText│ - Griglia magnetica e snapping  │ - Builder visuale di    │
│    Symbol, Link, Foil)  │ - Pan con mouse / spazio        │   regole 'VisibleWhen'  │
└─────────────────────────┴─────────────────────────────────┴─────────────────────────┘
```

### Versioning Immutabile dei Template (ADR-007)
Il Template Studio adotta il paradigma di versioning a rilascio:
- Tutte le modifiche vengono apportate su una versione bozza (`Draft`).
- Alla pressione di **Pubblica**, la bozza viene congelata con un numero di versione incrementale e contrassegnata come immutabile.
- Eventuali modifiche successive generano una nuova versione bozza; le carte esistenti salvate dagli utenti restano saldamente legate alla versione con cui sono state create, prevenendo rotture grafiche accidentali.
