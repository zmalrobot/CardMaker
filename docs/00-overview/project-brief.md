# Project Brief

## Obiettivo

CardMaker è un'applicazione professionale per la **creazione di carte da gioco personalizzate** ispirate a Yu-Gi-Oh!, Pokémon TCG e Magic: The Gathering.

Il principio fondante è:
> **Nessun asset grafico proprietario è incluso o generato dal programma.**  
> Frame, simboli, font, texture e retro carta sono **tutti caricati dall'amministratore** o generati tramite segnaposto procedurali geometrici. Questo evita qualsiasi violazione di copyright e rende il sistema estendibile a giochi arbitrari (anche inventati) senza scrivere nuovo codice.

---

## Principio Architetturale N.1

> Non si scrivono tre renderer. Si scrive **un solo motore di rendering data-driven** e ogni gioco, tipo di carta e template è **dato**, non codice.

Aggiungere un nuovo gioco significa semplicemente:
1. Caricare gli asset grafici e registrare i font;
2. Definire uno schema campi (`FieldDefinition`);
3. Disegnare i layout nell'editor visuale WYSIWYG.

---

## Requisiti Confermati dal Committente

| Ambito | Decisione |
|---|---|
| **Piattaforme** | **Web app + Desktop** (entrambe attive) |
| **Sistemi Desktop** | **Windows, macOS e Linux** (tramite Photino.Blazor) |
| **Esposizione Web** | **Pubblica su internet** → hardening e security headers obbligatori |
| **Utenti** | Multiutente con ruoli. In locale/desktop è attivo un bypass amministratore offline automatico |
| **Scala** | Cerchia privata (~10 utenti) con registrazione vincolata a invito |
| **Giochi Supportati** | **Yu-Gi-Oh!** (classico + Rush Duel), **Pokémon TCG**, **Magic: The Gathering** |
| **Layout Esotici** | Pendulum, Link, Token, Skill, Maximum Rush, split card, planeswalker |
| **Varianti e Foil** | Livelli di rarità e layer texture foil |
| **Retro Carta** | Gestione retro carta ed export fronte/retro combinato |
| **Editor Template** | Studio WYSIWYG a 3 pannelli (`/admin/templates/{id}`) |
| **Upload Asset** | Riservato agli amministratori, con verifica preventiva referenze |
| **Versioning Template** | Versioni pubblicate immutabili (Draft vs Published) |
| **Persistenza Carte** | Salvataggio collezione utente, modifica e duplicazione |
| **Risoluzioni Export** | **150 DPI** (web), **300 DPI** (stampa), **600 DPI** (alta fedeltà) |
| **Formati Export** | **PNG, JPEG, PDF** |
| **Stampa Professionale** | Solo spazio colore sRGB tipografico, nessun profilo CMYK né crocini di taglio |
| **Stack** | C# 13, .NET 10, SkiaSharp, SQLite WAL |
| **Rendering** | Server-side / in-process unificato (stesso motore per anteprima ed export) |
| **Lingua UI** | Italiano |

---

## Fuori Scope

- Generazione massiva da file CSV / Excel.
- Marketplace o condivisione pubblica delle carte.
- Motore di regole di gioco, simulazione, deck building o playtest.
- Generazione automatica di artwork tramite intelligenza artificiale.
- Applicazioni mobile native (iOS / Android).
- Imposizione tipografica complessa per tipografie commerciali (fogli macchina multi-carta, crocini manuali, CMYK).

---

## Flusso Utente

1. **Selezione Gioco**: L'utente sceglie tra Yu-Gi-Oh!, Pokémon o Magic.
2. **Selezione Tipo Carta**: Sceglie la tipologia (es. Mostro Effetto, Fase 1, Creatura).
3. **Selezione Template**: Sceglie la variante estetica o l'era del frame.
4. **Specializzazioni**: Seleziona tratti opzionali (Tuner, Toon, Subtype).
5. **Compilazione Form**: Inserisce titolo, carica e posiziona artwork, scrive effetti con token simboli `{sym:...}` e imposta valori numerici.
6. **Anteprima Live**: Visualizza in tempo reale il render a 60 FPS debouncato.
7. **Esportazione**: Scarica il file in formato PNG, JPEG o PDF a 150/300/600 DPI, con o senza abbondanza di stampa (Bleed 2 mm).
8. **Collezione**: La carta resta salvata nel profilo utente e può essere riaperta, modificata o duplicata.

---

## Flusso Amministratore

1. **Configurazione Gioco**: Definisce dimensioni fisiche (mm), raggio angoli e millimetri di abbondanza.
2. **Gestione Asset**: Carica frame, simboli, retro carta e texture foil nella Libreria Asset.
3. **Catalogo Font**: Registra font TTF/OTF associandoli ad alias di ruolo (`title`, `effect`, `stats`).
4. **Liste Opzioni**: Configura valori riutilizzabili (attributi, tipi mostro, elementi).
5. **Schema Campi**: Definisce i campi che gli utenti compileranno per ogni tipo di carta.
6. **Template Studio**: Progetta visualmente i layer, le coordinate e le condizioni logiche `VisibleWhen`.
7. **Pubblicazione**: Rilascia una versione immutabile del template.
