# AwesomePizza

## Decisioni

- **Monorepo Nx** — oggi sovradimensionato, ma consente di gestire in futuro FE backoffice e customer nello stesso repo.
- **Modello di dominio essenziale** — la traccia non richiede un modello più articolato della sola entità `Pizza`.
  - Un costo aggiuntivo per ingrediente extra avrebbe invece giustificato da subito un modello più ricco, per rappresentare la regola di prezzo.
  - La gestione degli allergeni sarà valutata come evoluzione, introducendo se necessario `Ingrediente`.
- **Stima dei tempi** — non è associata alla pizza: il tempo reale dipende dall'abilità del pizzaiolo e dal forno. Una stima per pizza sommerebbe approssimazioni senza renderla più affidabile.
