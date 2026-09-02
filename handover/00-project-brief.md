# 00 — Project Brief

## Obiettivo

Applicazione per la **creazione di carte da gioco personalizzate** ispirate a Yu-Gi-Oh!, Pokémon TCG
e Magic: The Gathering.

Principio fondante: **nessun asset grafico è incluso o generato dal programma**. Frame, simboli, font,
texture e retro carta sono **tutti caricati dall'amministratore**. Questo evita problemi di copyright e
rende il sistema estendibile a giochi arbitrari (anche inventati) senza scrivere codice.

## Principio architetturale n.1

> Non si scrivono tre renderer. Si scrive **un solo motore di rendering data-driven** e ogni gioco,
> tipo di carta e template è **dato**, non codice.

Aggiungere un nuovo gioco = caricare asset + definire uno schema campi + disegnare un layout nell'editor.

## Requisiti confermati dal committente

| Ambito | Decisione |
|---|---|
| Piattaforme | **Web app + Desktop** (entrambe) |
| Sistemi desktop | **Windows, macOS e Linux** |
| Esposizione web | **Pubblica su internet** → hardening obbligatorio |
| Utenti | Multiutente con account e ruoli. In **locale/desktop** esiste un **admin di default in bypass** |
| Scala | ~10 utenti (cerchia privata, registrazione a invito) |
| Gioco per la v1 | **Yu-Gi-Oh!** completo, **incluso Rush Duel** |
| Layout esotici | **Sì** (Pendulum, Link, Token, Skill, Maximum Rush, ecc.) |
| Varianti rarità / foil | **Sì** |
| Retro carta + export fronte/retro | **Sì** |
| Editor template admin | **WYSIWYG drag & drop** |
| Upload asset | **Solo admin** |
| Versioning dei template | **Sì** |
| Carte salvate / riapribili / modificabili | **Sì** |
| Generazione batch (CSV) | No |
| Galleria pubblica / condivisione | No |
| Risoluzione export | **600 DPI** |
| Formati export | **PNG, JPG, PDF** |
| Stampa professionale | **No** → solo sRGB, nessun CMYK, nessun crocino di taglio |
| Asset grafici | **Non esistono ancora**: verranno prodotti su specifica → [`06-asset-spec.md`](06-asset-spec.md) |
| Stack | **C# + SQLite** |
| Rendering | **Server-side** |
| Lingue UI | **Italiano + Inglese** |

## Fuori scope (v1)

- Generazione batch da CSV/Excel
- Galleria pubblica, condivisione social, commenti
- Marketplace / vendita di template
- Regole di gioco, simulazione, deck building, playtest
- Generazione automatica di artwork (AI o altro)
- App mobile nativa
- Preparazione per stampa professionale (CMYK, crocini, fogli imposti)

## Flusso utente finale

1. Sceglie il **gioco**
2. Sceglie il **tipo di carta**
3. Sceglie **template/variante** (rarità, edizione, era del frame)
4. Sceglie le **specializzazioni** (traits: Tuner, Toon, Spirit…)
5. Compila il **form dinamico** (titolo, immagine con crop, testo effetto, valori numerici)
6. Vede l'**anteprima live**
7. **Esporta** (PNG / JPG / PDF, fronte e retro, 600 DPI, con o senza abbondanza)
8. La carta resta **salvata** nella sua collezione e riapribile

## Flusso admin

1. Crea un **Gioco** e ne definisce le specifiche fisiche (mm, raggio angoli, abbondanza)
2. Carica gli **asset**: frame, simboli, font, texture foil, retro carta
3. Definisce **liste di opzioni** (razze, attributi, rarità…) associandovi i simboli
4. Definisce i **tipi di carta** e per ognuno lo **schema dei campi** che l'utente compilerà
5. Disegna i **template** nell'editor WYSIWYG, con layer e **regole condizionali**
6. **Pubblica** una versione del template (le carte già create restano legate alla versione con cui sono nate)

## Vincoli non funzionali

- Anteprima e export devono essere **pixel-identici** (stesso renderer, stessa pipeline).
- Un export a 600 DPI deve completarsi in tempi accettabili su hardware consumer.
- L'app desktop deve funzionare **completamente offline**.
- L'intero contenuto di un gioco deve essere **esportabile/importabile** come pacchetto singolo.
