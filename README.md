# AwesomePizza

## Avvio

Prerequisiti: .NET SDK 10, Node.js, Docker.

1. Dipendenze: `npm install`
2. Variabili d'ambiente: copiare `.env.example` in `.env` e impostare `MSSQL_SA_PASSWORD` (deve rispettare la policy di SQL Server: almeno 8 caratteri, maiuscole, minuscole, numeri e simboli).
3. Database: `docker compose up -d --wait`
4. Connection string (user secrets, non è nel repo; usare la stessa password del `.env`):
   ```
   dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=AwesomePizza;User Id=sa;Password=<password>;TrustServerCertificate=True" --project apps/api/api.csproj
   ```
5. Schema del database: `dotnet tool restore`, poi
   ```
   ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project libs/Infrastructure --startup-project apps/api
   ```
   (in PowerShell: impostare prima `$env:ASPNETCORE_ENVIRONMENT = "Development"`)
6. Avvio di API e web: `npm run dev`
   - API: http://localhost:5192, Swagger UI su `/swagger`
   - Web: http://localhost:4200

Test: `npm test`

## Decisioni

- **Monorepo Nx** — oggi sovradimensionato, ma consente di gestire in futuro FE backoffice e customer nello stesso repo.
- **Modello di dominio essenziale** — la traccia non richiede un modello più articolato della sola entità `Pizza`.
  - Un costo aggiuntivo per ingrediente extra avrebbe invece giustificato da subito un modello più ricco, per rappresentare la regola di prezzo.
  - La gestione degli allergeni sarà valutata come evoluzione, introducendo se necessario `Ingrediente`.
- **Stima dei tempi** — non è associata alla pizza: il tempo reale dipende dall'abilità del pizzaiolo e dal forno. Una stima per pizza sommerebbe approssimazioni senza renderla più affidabile.
- **Configurazione EF Core** — per il dominio attuale le Data Annotations sono sufficienti e leggibili; i vincoli sono costanti dell'entity, riutilizzate da attributi e validazioni. La Fluent API potrà essere introdotta se serviranno mapping più complessi.
- **Annullamento ordini** — non è previsto né implementato dai requisiti. Il codice pubblico è in sola lettura; per consentire in futuro l'annullamento mentre l'ordine è in coda, servirà un token segreto distinto, restituito alla creazione.
- **Stati d'ordine come enum** — gli stati guidano transizioni e scheduler, quindi aggiungerne uno richiede comunque codice: una tabella darebbe flessibilità solo apparente.
- **Snapshot della pizza in `OrderLine`** — nome e prezzo unitario restano quelli concordati al momento dell'ordine, anche se il catalogo cambia.
- **Ora stimata non persistita** — è derivata dallo stato della coda e ricalcolata a ogni lettura, così resta coerente senza aggiornamenti a cascata.
