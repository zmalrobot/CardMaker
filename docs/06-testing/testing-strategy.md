# Strategia di Collaudo e Test Automatizzati

La qualità, l'accuratezza geometrica e l'affidabilità della solution CardMaker sono garantite da una suite di collaudo automatizzata su **xUnit**, strutturata su più livelli piramidali.

---

## 1. Panoramica della Suite (200 Test Automatizzati)

Attualmente la suite comprende **200 test automatizzati**, suddivisi in due progetti indipendenti:

```text
tests/
├── CardMaker.Rendering.Tests/   # 107 test: Motore grafico, geometrie, calibrazioni tipografiche
└── CardMaker.Application.Tests/ #  93 test: Flussi applicativi E2E, storage, seeder, smoke test DI
```

Tutti i 200 test vengono eseguiti e superati con successo con `0` errori e `0` avvisi:
```bash
dotnet test CardMaker.slnx
```

---

## 2. Livelli di Test e Categorie

### A. Test Geometrici e Tipografici (`CardMaker.Rendering.Tests` — 107 Test)
- **Matematica delle Carte e Risoluzioni**: Verifica delle formule di conversione millimetri/pixel a 150, 300 e 600 DPI per formati Standard Poker (63 × 88 mm) e Japanese (59 × 86 mm).
- **Aree Tipografiche**: Verifica delle tolleranze di *Bleed Box* (2 mm per lato), *Trim Box* e *Safe Zone* (3 mm).
- **Motore Tipografico (`TextEngine`)**: Auto-fit orizzontale (*shrink*, *condense*, *shrink-and-condense*), wrapping su caselle multiriga, calibrazione del centraggio ottico su `CapHeight`.
- **Parsing Glifi Inline**: Riconoscimento ed estrazione dei token `{sym:...}` per Yu-Gi-Oh!, energie Pokémon e mana MTG.
- **Strategy Painters**: Test specifici per ciascun painter (frame, artwork con crop, frecce Link, ripetitori di stelle, layer foil).
- **Esportazione PDF**: Verifica della generazione vettoriale multipagina e conformità dei metadati.

### B. Test di Dominio e Applicativi (`CardMaker.Application.Tests` — 93 Test)
- **Ciclo di Vita Carta E2E**: Creazione carta, validazione schema campi, anteprima PNG, salvataggio e duplicazione.
- **Content Graph Seeder**: Idempotenza del popolamento database per Yu-Gi-Oh!, Pokémon e Magic.
- **Storage Content-Addressed**: Verifica dell'hash SHA-256, deduplicazione dei binari e isolamento da *Path Traversal*.
- **Cancellazione Sicura**: Verifica del blocco eliminazione asset in presenza di referenze attive in template o carte.
- **Sicurezza Upload**: Rifiuto di payload non consentiti, file privi di estensione o font compressi non supportati (`.woff2`).
- **Smoke Test di Dependency Injection**: Verifica che l'intero container dei servizi si costruisca correttamente sia per la configurazione Desktop che per la configurazione Web.

---

## 3. Esecuzione Selettiva dei Test

```bash
# Esecuzione completa di tutti i test
dotnet test CardMaker.slnx

# Esecuzione esclusiva dei test di rendering
dotnet test tests/CardMaker.Rendering.Tests/CardMaker.Rendering.Tests.csproj

# Esecuzione esclusiva dei test applicativi
dotnet test tests/CardMaker.Application.Tests/CardMaker.Application.Tests.csproj

# Esecuzione filtrata per classe o pattern di nome
dotnet test --filter FullyQualifiedName~TextEngine
dotnet test --filter FullyQualifiedName~DesktopAndWebSmokeTests
```
