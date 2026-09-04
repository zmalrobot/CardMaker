# Piano dei modelli AI: quale usare per quale fase

Guida per ottimizzare l'utilizzo della quota AI durante lo sviluppo.

> [!NOTE]
> I moltiplicatori di costo dei modelli cambiano nel tempo. Le indicazioni qui sono sul **tipo di lavoro**,
> non su numeri fissi. Verifica il consumo reale nelle impostazioni del tuo account.

---

## 1. Regola generale

| Usa il modello **potente** quando… | Usa il modello **intermedio** quando… |
|---|---|
| il problema è aperto e va progettato | il "cosa fare" è già deciso e va eseguito |
| l'errore costa caro da scoprire dopo (sicurezza, modello dati, algoritmi) | l'errore si vede subito (compilazione, layout, test) |
| serve capire *perché* qualcosa non funziona | serve applicare una correzione già individuata |
| si sta definendo un'API o un contratto | si sta implementando dietro un'API esistente |
| il lavoro è matematico o geometrico | il lavoro è ripetitivo o meccanico |

**Sintesi:** 🔴 pensare → modello potente. 🟡 digitare → modello intermedio.

---

## 2. Fase → modello consigliato

| Fase | Modello consigliato | Perché |
|---|---|---|
| **F2** — Layer avanzati | 🔴 Potente per il parser `richText` con simboli inline, il crop "a fetta" e i golden test<br>🟡 Intermedio per `symbolRepeater`, `toggleGroup`, export PDF | Il parser inline e l'allineamento alla baseline sono la parte sottile; i ripetitori di simboli sono meccanici |
| **F3** — Seed Yu-Gi-Oh! | 🟡 Intermedio<br>🔴 Potente solo per decidere se Rush è un `Game` separato | Quasi tutto è inserimento di dati: 25 razze, 10 rarità, 26 template |
| **F4** — Design system e temi | 🔴 Potente per impostare token e struttura (una sessione)<br>🟡 Intermedio per tutto il CSS e i componenti | Le scelte di architettura CSS si pagano care se sbagliate; scrivere i componenti no |
| **F5** — Flusso utente | 🔴 Potente per il motore del form dinamico da `FieldDefinition`<br>🟡 Intermedio per wizard, schermate, export | Il form dinamico è generico e va progettato bene; le schermate sono lavoro noto |
| **F6** — Admin contenuti | 🟡 Intermedio | CRUD su entità già modellate |
| **F7** — Editor WYSIWYG | 🔴 Potente | Matematica di drag/resize/snap, undo/redo, builder visuale delle regole: è la fase più difficile |
| **F8** — Host desktop | 🔴 Potente per il primo setup Photino cross-platform<br>🟡 Intermedio per il resto | Le insidie native (WebKitGTK, percorsi dati, publish) sono difficili da diagnosticare |
| **F9** — Hardening | 🔴 Potente | Sicurezza: un errore qui non lo vedi finché non è tardi |
| **F10** — Rifiniture | 🟡 Intermedio | Localizzazione, pagine statiche, gestione utenti |
| **F11** — Pokémon | 🔴 Potente per il `repeatingBlock` ad altezza variabile<br>🟡 Intermedio per seed e template | Il layout a flusso è l'ultima capacità difficile del motore |
| **F12** — Magic | 🔴 Potente per il parser dei simboli di mana e i layout esotici<br>🟡 Intermedio per seed e template | La grammatica `{2}{W/U}{X}` e le split card richiedono progettazione |

---

## 3. Tipo di attività → modello

Vale dentro qualsiasi fase, e conta più della tabella sopra.

| Attività | Modello |
|---|---|
| "Progetta come modellare X" | 🔴 |
| "Perché questo render è sbagliato?" | 🔴 |
| "Rivedi la sicurezza di questo endpoint" | 🔴 |
| "Questo algoritmo di auto-fit è corretto?" | 🔴 |
| "Aggiungi una pagina CRUD per le liste opzioni" | 🟡 |
| "Scrivi il CSS del tema scuro" | 🟡 |
| "Aggiungi i 25 tipi di mostro al seed" | 🟡 |
| "Correggi questi 6 errori di compilazione" | 🟡 |
| "Rinomina questo simbolo ovunque" | 🟡 |
| "Scrivi i test per questa classe" | 🟡 |
| "Aggiorna la documentazione" | 🟡 |

---

## 4. Sequenza di sessioni consigliata

Una sessione per fase, non una sessione per tutto il progetto: il contesto lungo costa a ogni messaggio.

```
Sessione 1  🔴  F2 — progetta il parser richText, poi implementalo
Sessione 2  🟡  F2 — symbolRepeater, toggleGroup, export PDF, test
Sessione 3  🟡  F3 — seed Yu-Gi-Oh! (dati)
Sessione 4  🔴  F4 — token, struttura CSS, tema chiaro/scuro (solo impostazione)
Sessione 5  🟡  F4 — componenti e migrazione delle pagine esistenti
Sessione 6  🔴  F5 — motore del form dinamico
Sessione 7  🟡  F5 — wizard, anteprima live, export
...
```

Ogni sessione si apre leggendo `docs/02-development/resume-prompt.md` e si chiude aggiornando la
documentazione pertinente. Così la successiva riparte leggera invece di trascinarsi tutto il contesto.

---

## 5. Come consumare meno, qualunque modello

1. **Non incollare i file in chat.** Di': *"leggi `docs/00-overview/project-context.md`"*. L'assistente li
   apre da solo e legge solo ciò che gli serve.
2. **Una fase per sessione.** Ogni messaggio ripaga l'intera conversazione precedente: le chat lunghe
   costano progressivamente di più.
3. **Chiedi modifiche mirate**, non "sistema tutto".
4. **Non far rieseguire build e test a raffica.** Falli eseguire una volta a fine modifica.
5. **Chiudi sempre la fase aggiornando la documentazione.** È ciò che rende sicuro ripartire da zero.
6. **Le domande brevi non meritano il modello potente.** "Come si chiama quel file?" → intermedio.
7. **Se una risposta ti convince a metà, non ripartire da capo:** chiedi la correzione puntuale.

---

## 6. Quando cambiare idea

Alza al modello potente se noti che quello intermedio:

- gira in torno sullo stesso errore per più di due tentativi;
- propone soluzioni che ignorano vincoli scritti nella documentazione;
- tocca file di architettura (`CardRenderer`, `TextEngine`, `CardMakerDbContext`, i modelli di layout).

Abbassa al modello intermedio appena il lavoro diventa "applica questa decisione in N punti".

