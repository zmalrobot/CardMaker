# Database, Migrazioni e Backup

CardMaker utilizza **SQLite** gestito tramite **Entity Framework Core 10** come motore di persistenza primario.

---

## 1. Configurazione SQLite WAL

Al fine di garantire elevate prestazioni in scenari concorrenti (come anteprime parallele e letture multiple mentre l'amministratore modifica i template o l'utente salva una carta), la connessione SQLite è configurata in modalità **WAL** (*Write-Ahead Logging*):

```csharp
// Configurazione in CardMakerDbContext / DatabaseInitializer
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA foreign_keys = ON;
```

### Caratteristiche della Modalità WAL
- I lettori non bloccano gli scrittori.
- Gli scrittori non bloccano i lettori.
- Le transazioni di scrittura creano append-only log (`CardMaker.db-wal`), rendendo i salvataggi veloci e atomici.

---

## 2. Migrazioni EF Core

Il database viene generato e aggiornato deterministicamente tramite il meccanismo di code-first migrations di EF Core.
Le classi di migrazione risiedono in:
```text
src/CardMaker.Infrastructure/Persistence/Migrations/
```

All'avvio dell'applicazione (`Program.cs` sia in Desktop che in Web), `DatabaseInitializer.InitializeAsync()` esegue automaticamente `db.Database.MigrateAsync()`, applicando eventuali migrazioni pendenti prima di servire richieste.

### Creazione di Nuove Migrazioni
```bash
dotnet ef migrations add <NomeDescrizione> \
  --project src/CardMaker.Infrastructure \
  --startup-project src/CardMaker.Web \
  --output-dir Persistence/Migrations
```

---

## 3. Seeding Idempotente dei Dati

Dopo l'applicazione delle migrazioni, `DatabaseInitializer` delega il popolamento iniziale ai seeder di contenuto:
1. **Utente Amministratore**: Se la tabella utenti è vuota, crea l'utente amministratore predefinito.
2. **Seeding Giochi e Font**:
   - `YuGiOhContentSeeder` & `YuGiOhFontSeeder`
   - `PokemonContentSeeder` & `PokemonFontSeeder`
   - `MtgContentSeeder` & `MtgFontSeeder`
3. **Seeding Placeholder**: `PlaceholderSeeder` genera proceduralmente le immagini PNG dei frame e dei simboli segnaposto per consentire il collaudo immediato senza richiedere il caricamento manuale di asset protetti da copyright.
   - L'operazione è parallelizzata tramite `Parallel.For` con pre-indicizzazione in memoria per azzerare i tempi di avvio (ottimizzazione `PERF-001`).

---

## 4. Snapshot Online e Backup (`VACUUM INTO`)

Per generare backup affidabili senza arrestare l'applicazione né incorrere in lock o corruzioni di file aperti, CardMaker implementa `SqliteDatabaseSnapshotProvider`:

```csharp
// Esecuzione atomica dello snapshot online
await db.Database.ExecuteSqlRawAsync($"VACUUM INTO '{targetBackupFilePath}'");
```

### Verifica di Integrità
Subito dopo la generazione del file di snapshot, il servizio esegue sul file di destinazione il controllo formale:
```sql
PRAGMA integrity_check;
```
Se il controllo non restituisce `ok`, il backup viene contrassegnato come non valido e rimosso, garantendo che ogni archivio generato sia matematicamente integro e ripristinabile.
