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

## Modello della cucina

Una postazione (`Workstation`) abbina un pizzaiolo (`Baker`) e un forno elettrico (`Oven`), descritti da quattro parametri:

- **`PreparationTimePerPizza`** — tempo medio per stendere e condire una pizza. Il pizzaiolo lavora una pizza alla volta.
- **`MaxConcurrentPizzas`** — pizze aperte che il pizzaiolo segue insieme, sul banco e in forno: una pizza è aperta da quando inizia a stenderla a quando esce dal forno.
- **`Capacity`** — posti in forno.
- **`BakingTime`** — tempo in cui un'infornata resta in forno, comprese infornata e sfornata.

L'abilità del pizzaiolo è quindi descritta da velocità (`PreparationTimePerPizza`) e pizze gestibili insieme (`MaxConcurrentPizzas`); il forno aggiunge capienza e tempo di cottura.

- **Infornata (`Batch`)** — gruppo di pizze che entra ed esce dal forno insieme. Contiene al massimo `min(MaxConcurrentPizzas, Capacity)` pizze; più infornate cuociono insieme se i posti bastano. È l'unità di lavoro assegnata al pizzaiolo: «prepara questi ordini, N pizze».
- **Composizione** — un ordine non si spezza se ci sta; un ordine più grande dell'infornata massima si divide in infornate consecutive e il resto può condividere l'infornata con altri ordini. `BatchLine` punta alla riga d'ordine (`OrderLine`), così il pizzaiolo sa quali pizze preparare anche per un ordine diviso. Un'infornata non aspetta di riempirsi e la sua composizione è fissata all'assegnazione: un ordine arrivato dopo va nell'infornata successiva.
- **Politica di pianificazione** — l'ordine e la composizione delle infornate future sono una politica configurabile (pattern Strategy): FIFO, oppure FIFO con backfill, dove un ordine successivo più piccolo può riempire i posti liberi se nessun ordine peggiora la sua ora stimata di più di una tolleranza T rispetto al piano FIFO (EASY/conservative backfilling). Con T = 0 nessun ordine peggiora rispetto a FIFO, ma il backfill può comunque anticipare un ordine quando non costa nulla a nessuno (per esempio con il forno già occupato), quindi le due politiche non coincidono. T è impostato a 2 minuti.
- **Assegnazione** — la prima infornata pianificata viene assegnata quando il pizzaiolo può iniziarla: ha finito di preparare la precedente e ha almeno una pizza aperta disponibile. I suoi ordini passano a `InPreparation` e l'infornata viene salvata; `StartedAt` è l'istante di assegnazione. Un ordine è pronto quando è uscita l'ultima infornata che lo contiene.
- **Calcolo (`BatchTimeEstimator`)** — funzione pura del dominio (functional core, imperative shell): riceve pizzaiolo, forno, infornate in ordine e ora corrente e restituisce inizio, ingresso in forno e uscita stimati di ogni infornata. Le pizze di un'infornata si preparano in serie, ciascuna quando il pizzaiolo ha le mani libere e una pizza aperta disponibile; l'infornata entra in forno quando tutte le sue pizze sono pronte e ci sono abbastanza posti; all'uscita si liberano insieme posti e pizze aperte. Il pizzaiolo prepara l'infornata successiva mentre la precedente cuoce, nei limiti delle pizze aperte. Il problema è un flow shop a due stadi con buffer limitato; con tempi uniformi e ordine dato si risolve con un list scheduling lineare.
- **Click del pizzaiolo** — valgono per l'infornata. La presa in carico è facoltativa e corregge l'inizio (`TakenAt ?? StartedAt`); «pronta» è obbligatoria. Un'infornata che secondo il piano doveva essere uscita ma non è confermata si considera in uscita adesso, e tiene occupati posti e pizze aperte fino ad allora: un pizzaiolo in ritardo non riceve altro lavoro e le ore stimate successive slittano.

### Colli di bottiglia

Il ritmo della postazione dipende da quale risorsa produce meno pizze al minuto. Poiché le pizze in forno contano tra le pizze aperte, il forno può limitare solo se `MaxConcurrentPizzas` supera `Capacity`.

| Regime | Profilo di riferimento | Comportamento |
|---|---|---|
| Gestione del pizzaiolo | Junior: 3' a pizza, 4 pizze aperte; forno da 8 posti, 4' | Durante la cottura ha già tutte le pizze aperte in forno e resta fermo: 4 pizze ogni 16' |
| Mani del pizzaiolo | Esperto: 1' a pizza, 12 pizze aperte; forno da 8 posti, 4' | Prepara la successiva mentre la precedente cuoce e non si ferma mai: 8 pizze ogni 8', forno al 50% |
| Forno | Esperto con forno da 6 posti e 8' | Le infornate pronte aspettano posti liberi: 6 pizze ogni 8' |

Con numeri realistici un solo pizzaiolo raramente riempie un forno da 6–9 posti: è il motivo per cui nelle pizzerie più pizzaioli condividono lo stesso forno.

## Decisioni

- **Monorepo Nx** — oggi sovradimensionato, ma consente di gestire in futuro FE backoffice e customer nello stesso repo.
- **Modello di dominio essenziale** — la traccia non richiede un modello più articolato della sola entità `Pizza`.
  - Un costo aggiuntivo per ingrediente extra avrebbe invece giustificato da subito un modello più ricco, per rappresentare la regola di prezzo.
  - La gestione degli allergeni sarà valutata come evoluzione, introducendo se necessario `Ingrediente`.
- **Stima dei tempi** — non è associata alla pizza: il tempo reale dipende dall'abilità del pizzaiolo e dal forno. Una stima per pizza sommerebbe approssimazioni senza renderla più affidabile.
- **Configurazione EF Core** — per il dominio attuale le Data Annotations sono sufficienti e leggibili; i vincoli sono costanti dell'entity, riutilizzate da attributi e validazioni. La Fluent API potrà essere introdotta se serviranno mapping più complessi.
- **Collezioni** — parametri e valori restituiti del dominio sono `IReadOnlyList`: il piano delle infornate è calcolato per intero in memoria e letto più volte (stime di tutti gli ordini, assegnazione della prima infornata), quindi una lista di sola lettura è il tipo più preciso. `IEnumerable` resta riservato ai flussi davvero lazy, che qui non servono: la coda di una pizzeria è di poche decine di ordini.
- **Annullamento ordini** — non è previsto né implementato dai requisiti. Il codice pubblico è in sola lettura; per consentire in futuro l'annullamento mentre l'ordine è in coda, servirà un token segreto distinto, restituito alla creazione.
- **Stati d'ordine come enum** — gli stati guidano transizioni e scheduler, quindi aggiungerne uno richiede comunque codice: una tabella darebbe flessibilità solo apparente.
- **Snapshot della pizza in `OrderLine`** — nome e prezzo unitario restano quelli concordati al momento dell'ordine, anche se il catalogo cambia.
- **Ora stimata non persistita** — è derivata dallo stato della coda e ricalcolata a ogni lettura, così resta coerente senza aggiornamenti a cascata. Una lettura carica postazione, infornate aperte e ordini non pronti (poche decine di righe, coperte dagli indici) e un solo calcolo serve stato dell'ordine, bacheca, vista del pizzaiolo e assegnazione. Il calcolo resta in C# e non in SQL: è sequenziale, mantiene lo stato di pizze aperte e posti, e così si verifica con test di unità.
- **Infornate assegnate persistite** — un'infornata assegnata è un fatto, come l'inizio di un ordine: salvarla evita che un ricalcolo ne cambi la composizione mentre il pizzaiolo la sta preparando. Le infornate future si calcolano.
- **Presa in carico facoltativa, per infornata** — la stima si basa sul piano dello scheduler, oppure sull'inizio reale se il pizzaiolo registra la presa in carico. Sta sull'infornata e non sull'ordine perché le pizze entrano ed escono insieme: due ordini della stessa infornata con prese in carico diverse sarebbero fisicamente impossibili. Scartato un margine di attesa: avrebbe rilevato prima i ritardi, ma un click dimenticato avrebbe bloccato nuovi ordini.
- **Ordine pronto = evaso** — l'evasione coincide con la disponibilità al ritiro, coerentemente con il focus della traccia sulla cucina, sulla coda e sui tempi stimati.
- **Nessun cambio di pizzaiolo a metà infornata** — `AssignBaker` è consentito solo se la postazione non ha infornate aperte: un'infornata assegnata potrebbe superare i limiti del nuovo pizzaiolo.
- **Forno a legna escluso** — questa iterazione assume un forno elettrico: durante la cottura senza presidio, il pizzaiolo può preparare l'infornata successiva. Con il forno a legna la cottura dura 60–90 secondi, la pizza va girata di continuo e spesso si cuoce una pizza alla volta con la pala; se il pizzaiolo presidia il forno, preparazione e cottura si alternano anziché sovrapporsi.

### Limiti accettati

- Le infornate pronte escono dal calcolo: dopo una conferma in ritardo, l'infornata successiva può risultare per pochi minuti «in uscita adesso» prima di essere corretta dal click successivo.
- Senza presa in carico vale il piano, e il ritardo del pizzaiolo si vede solo quando l'ora stimata scade.
- Con poco lavoro le infornate sono piccole: un ordine non aspetta altri ordini per riempire il forno.
- Con il backfill, l'ora stimata di un ordine in coda può peggiorare fino a T per far passare ordini più piccoli; con FIFO non succede.

## Evoluzioni

- **Più pizzaioli e forni** — più pizzaioli che condividono lo stesso forno, con un fornaio dedicato nelle pizzerie con volumi alti; è lo scenario in cui la capienza del forno conta davvero.
- **Forno a legna** — un parametro sul forno che indichi se richiede presidio, oppure il fornaio come risorsa a sé.
- **Tempi per tipo di pizza** e **calibrazione dai dati reali**: aggiornare tempi di preparazione e cottura da `TakenAt`/`ReadyAt` delle infornate, anche per fascia oraria.
- **Ore stimate persistite** — con volumi alti o più istanze dell'API, calcolarle a ogni evento e salvarle, così le letture diventano query semplici.
- **Annullamento e stato `Completed`** — per tracciare ritiro o consegna dopo `Ready`.
