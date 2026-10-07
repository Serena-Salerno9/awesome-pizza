# Awesome Pizza

Prima iterazione del portale ordini: API REST per ordinare pizze senza registrazione e gestire la coda del pizzaiolo, con documentazione Swagger. Stack: .NET 10, C#, EF Core, SQL Server. Il codice è in `libs/` (Domain, Application, Infrastructure, test) e `apps/api`.

## Avvio

Prerequisiti: .NET SDK 10, Node.js, Docker.

1. `npm install`
2. Copiare `.env.example` in `.env` e impostare `MSSQL_SA_PASSWORD`.
3. `docker compose up -d --wait`
4. Connection string nei user-secrets: `dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=AwesomePizza;User Id=sa;Password=<password>;TrustServerCertificate=True" --project apps/api/api.csproj`
5. `dotnet tool restore`, poi `dotnet ef database update --project libs/Infrastructure --startup-project apps/api` (con `ASPNETCORE_ENVIRONMENT=Development`).
6. `dotnet run --project apps/api --launch-profile http`: Swagger su http://localhost:5192/swagger.

Test: `npm test`. Serve il container attivo, perché i test dei servizi usano il database `AwesomePizza_Tests`.

## Analisi dei requisiti

- **Cliente senza account.** Dopo l'ordine riceve un codice giornaliero (`001`) con cui consulta stato e ora stimata.
- **Capacità della cucina.** Una postazione abbina pizzaiolo e forno, descritti da quattro parametri: tempo di preparazione per pizza, pizze gestibili insieme, posti del forno e tempo di cottura. Sono i due limiti della traccia: abilità e capienza.
- **Infornata.** Al pizzaiolo passo gruppi di pizze che entrano ed escono dal forno insieme, entro il più restrittivo dei due limiti. Un ordine grande si divide, uno che ci sta non si spezza. Così il sistema indica quanti ordini e pizze preparare.
- **Ora stimata.** È calcolata a ogni lettura dalla coda e non salvata, così non diventa incoerente (`BatchTimeEstimator`, funzione pura testata). Un ordine è evaso quando esce l'ultima infornata che lo contiene.
- **Assegnazione.** Un servizio in background assegna la prossima infornata appena il pizzaiolo può iniziarla (ogni 5 secondi). Lui la vede in `GET /api/v1/kitchen` e può segnare «presa in carico» (facoltativa) e «pronta» (obbligatoria). Se un'infornata non è confermata «pronta» dopo l'ora prevista, non ne parte nessun'altra e le ore stimate di quelle dietro slittano a ogni minuto che passa.
- **Pianificazione.** FIFO con backfill: un ordine piccolo passa avanti se nessun altro slitta di più di 5 minuti. In alternativa FIFO semplice (`Planning:Policy`).

## Requisiti e codice

| Requisito | Dove |
|---|---|
| Ordini senza registrazione | `POST /api/v1/orders`, `OrderService` |
| Limiti di pizzaiolo e forno | `libs/Domain/Kitchen` |
| Ordini e pizze per il pizzaiolo | `GET /api/v1/kitchen`, `KitchenPlanner` |
| Codice, stato e ora stimata | `GET /api/v1/orders/{code}`, `BatchTimeEstimator` |
| Ordini in preparazione e successivi | `GET /api/v1/orders` |
| Documentazione automatica | Swagger UI, `apps/api/Program.cs` |
| Test di unità | `libs/Domain.Tests`, `libs/Application.Tests` |

## Esempio d'uso

Da Swagger: `GET /api/v1/menu` → `POST /api/v1/orders` con `{"items":[{"pizzaId":"<id>","quantity":2}]}` → entro 5 secondi l'ordine è `InPreparation` → `GET /api/v1/kitchen` mostra l'infornata con il `batchId` → `POST /api/v1/kitchen/batches/{id}/ready` → `GET /api/v1/orders/001` risponde `Ready`.

## Limiti noti

- Una sola postazione e forno elettrico: con il forno a legna il pizzaiolo non potrebbe preparare durante la cottura.
- Nessun annullamento dell'ordine.
- Un ritardo del pizzaiolo emerge solo alla scadenza dell'ora stimata.

## Con più tempo a disposizione

- Autenticazione e ruoli, con le chiamate del pizzaiolo riservate.
- Più pizzaioli e forni, forno a legna.
- Tempi per tipo di pizza, calibrati sui dati reali.
- Ore stimate salvate e assegnazione protetta con più istanze dell'API.
- Annullamento dell'ordine con token segreto e stato `Completed`.
- Frontend React: tabellone per i clienti e vista del pizzaiolo.
- Notifiche al cliente, CI/CD, log e metriche, test end-to-end e di carico.
