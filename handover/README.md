# CardMaker — Cartella di Handover

Questa cartella è la **memoria persistente del progetto**. Se cambi sessione di lavoro (o assistente AI),
leggere questi file in ordine è sufficiente per ricostruire tutto il contesto.

## Come usarla

1. Leggi sempre per primo [`STATE.md`](STATE.md) → dove siamo e qual è il prossimo passo.
2. Poi i documenti numerati, in ordine, solo se serve approfondire.
3. **Al termine di ogni fase** vanno aggiornati: `STATE.md`, `04-roadmap.md` e, se sono state prese
   scelte non banali, `05-decisions.md`.

> **Cambio di computer?** Vai direttamente a [`08-resume-prompt.md`](08-resume-prompt.md).

## Indice

| File | Contenuto |
|---|---|
| [`STATE.md`](STATE.md) | Stato corrente, fase attiva, prossimo passo, blocchi aperti |
| [`00-project-brief.md`](00-project-brief.md) | Obiettivo del progetto, requisiti confermati dal committente, vincoli |
| [`01-card-anatomy.md`](01-card-anatomy.md) | Analisi di dominio: anatomia carte Yu-Gi-Oh!, Pokémon, Magic |
| [`02-architecture.md`](02-architecture.md) | Stack tecnologico, struttura della solution, pipeline di rendering, sicurezza |
| [`03-data-model.md`](03-data-model.md) | Entità del database e schema JSON del layout dei template |
| [`04-roadmap.md`](04-roadmap.md) | Fasi di implementazione con criteri di completamento |
| [`05-decisions.md`](05-decisions.md) | Registro delle decisioni architetturali (ADR) con motivazioni |
| [`06-asset-spec.md`](06-asset-spec.md) | **Specifica per il grafico**: formati, misure, nomi e elenco completo degli asset da produrre |
| [`07-dev-guide.md`](07-dev-guide.md) | Comandi, primo avvio, dove finiscono i dati, debito tecnico noto |
| [`08-resume-prompt.md`](08-resume-prompt.md) | **Come riprendere il lavoro su un PC nuovo**: cosa copiare, prerequisiti, prompt pronto da incollare |
| [`09-model-plan.md`](09-model-plan.md) | Quale modello AI usare per quale fase, per non esaurire la quota |

## Regole di manutenzione

- I documenti descrivono **il progetto**, non la cronologia delle chat.
- Se una decisione cambia, si **aggiorna** l'ADR esistente marcandolo come *Superseded* e se ne aggiunge uno nuovo: non si cancella la storia.
- Niente segreti, password o connection string reali in questi file.
