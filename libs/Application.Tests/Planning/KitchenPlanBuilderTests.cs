using Application.Planning;
using Domain.Kitchen;
using Domain.Kitchen.Planning;

namespace Application.Tests.Planning;

public class KitchenPlanBuilderTests
{
  private static readonly DateTimeOffset Noon = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

  private static DateTimeOffset At(int minutes) => Noon.AddMinutes(minutes);

  // Forno piccolo: il massimo per infornata è 6 pizze, 1' di preparazione, 8' di cottura. L'ora attuale è At(0)
  private static readonly Baker TestBaker = new("Mario", 16, TimeSpan.FromMinutes(1));
  private static readonly Oven SmallOven = new(6, TimeSpan.FromMinutes(8));

  private static OrderLineSnapshot Line(string pizzaName, int quantity, int assigned = 0) =>
    new(Guid.NewGuid(), pizzaName, quantity, assigned);

  private static OrderSnapshot CreateOrder(string code, int createdAtMinutes, params OrderLineSnapshot[] lines) =>
    new(Guid.NewGuid(), code, At(createdAtMinutes), lines);

  private static BatchSnapshot OpenBatch(int earliestStartMinutes, params (OrderSnapshot Order, OrderLineSnapshot Line, int Quantity)[] items) =>
    new(
      Guid.NewGuid(),
      At(earliestStartMinutes),
      items.Select(i => new BatchLineSnapshot(i.Line.Id, i.Order.Id, i.Line.PizzaName, i.Quantity)).ToList());

  private static QueueSnapshot CreateSnapshot(OrderSnapshot[] orders, params BatchSnapshot[] openBatches) =>
    new(TestBaker, SmallOven, At(0), openBatches, orders);

  private static KitchenPlanBuilder CreateBuilder(IBatchPlanner? planner = null) =>
    new(planner ?? new FifoBatchPlanner());

  private static DateTimeOffset ReadyAt(KitchenPlan plan, OrderSnapshot order) =>
    plan.Orders.Single(o => o.OrderId == order.Id).EstimatedReadyAt;

  // Planner finto: registra ciò che riceve e non pianifica nulla
  private sealed class SpyPlanner : IBatchPlanner
  {
    public IReadOnlyList<PendingOrder>? ReceivedQueue { get; private set; }
    public PlanningContext? ReceivedContext { get; private set; }

    public IReadOnlyList<PlannedBatch> Plan(IReadOnlyList<PendingOrder> queue, PlanningContext context)
    {
      ReceivedQueue = queue;
      ReceivedContext = context;
      return [];
    }
  }

  #region Validazione

  [Fact]
  public void Constructor_WithNullPlanner_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new KitchenPlanBuilder(null!));
  }

  [Fact]
  public void Build_WithNullSnapshot_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => CreateBuilder().Build(null!));
  }

  [Fact]
  public void Build_WithNoOrders_ReturnsEmptyPlan()
  {
    var plan = CreateBuilder().Build(CreateSnapshot([]));

    Assert.Empty(plan.Batches);
    Assert.Empty(plan.Orders);
  }

  #endregion

  #region Coda per il planner

  [Fact]
  public void Build_QueuesOrdersByCreationTime()
  {
    var first = CreateOrder("001", 0, Line("Margherita", 1));
    var second = CreateOrder("002", 5, Line("Diavola", 1));
    var spy = new SpyPlanner();

    CreateBuilder(spy).Build(CreateSnapshot([second, first]));

    Assert.Equal([first.Id, second.Id], spy.ReceivedQueue!.Select(o => o.OrderId));
  }

  [Fact]
  public void Build_WithSameCreationTime_QueuesOrdersByCode()
  {
    var first = CreateOrder("001", 0, Line("Margherita", 1));
    var second = CreateOrder("002", 0, Line("Diavola", 1));
    var spy = new SpyPlanner();

    CreateBuilder(spy).Build(CreateSnapshot([second, first]));

    Assert.Equal([first.Id, second.Id], spy.ReceivedQueue!.Select(o => o.OrderId));
  }

  [Fact]
  public void Build_QueuesOnlyPendingPizzasOfPartiallyAssignedLines()
  {
    var line = Line("Margherita", 4, assigned: 3);
    var spy = new SpyPlanner();

    CreateBuilder(spy).Build(CreateSnapshot([CreateOrder("001", 0, line)]));

    Assert.Equal([new PendingLine(line.Id, 1)], Assert.Single(spy.ReceivedQueue!).Lines);
  }

  [Fact]
  public void Build_SkipsFullyAssignedLinesOfAnOrder()
  {
    var done = Line("Margherita", 2, assigned: 2);
    var todo = Line("Diavola", 3);
    var spy = new SpyPlanner();

    CreateBuilder(spy).Build(CreateSnapshot([CreateOrder("001", 0, done, todo)]));

    Assert.Equal([new PendingLine(todo.Id, 3)], Assert.Single(spy.ReceivedQueue!).Lines);
  }

  [Fact]
  public void Build_SkipsOrdersWithoutPendingPizzas()
  {
    var finished = CreateOrder("001", 0, Line("Margherita", 2, assigned: 2));
    var waiting = CreateOrder("002", 1, Line("Diavola", 1));
    var spy = new SpyPlanner();

    CreateBuilder(spy).Build(CreateSnapshot([finished, waiting]));

    Assert.Equal([waiting.Id], spy.ReceivedQueue!.Select(o => o.OrderId));
  }

  #endregion

  #region Contesto per il planner

  [Fact]
  public void Build_PassesKitchenStateToPlanner()
  {
    var order = CreateOrder("001", -5, Line("Margherita", 4, assigned: 4));
    var open = OpenBatch(-2, (order, order.Lines[0], 4));
    var spy = new SpyPlanner();

    CreateBuilder(spy).Build(CreateSnapshot([order], open));

    var context = spy.ReceivedContext!;
    Assert.Equal(6, context.MaxBatchSize);
    Assert.Same(TestBaker, context.Kitchen!.Baker);
    Assert.Same(SmallOven, context.Kitchen.Oven);
    Assert.Equal(At(0), context.Kitchen.Now);
    Assert.Equal([new BatchToEstimate(4, At(-2))], context.Kitchen.AssignedBatches);
  }

  #endregion

  #region Infornate del piano

  [Fact]
  public void Build_WithOneOrder_PlansOneBatchWithEstimatedTimes()
  {
    var order = CreateOrder("001", 0, Line("Margherita", 3));

    var plan = CreateBuilder().Build(CreateSnapshot([order]));

    var batch = Assert.Single(plan.Batches);
    Assert.Null(batch.BatchId);
    Assert.Equal(At(0), batch.EstimatedStartAt);
    Assert.Equal(At(3), batch.EstimatedBakeStartAt);
    Assert.Equal(At(11), batch.EstimatedReadyAt);
  }

  [Fact]
  public void Build_BatchLinesCarryOrderAndPizzaName()
  {
    var margherita = Line("Margherita", 2);
    var diavola = Line("Diavola", 1);
    var order = CreateOrder("001", 0, margherita, diavola);

    var plan = CreateBuilder().Build(CreateSnapshot([order]));

    Assert.Equal(
      [
        new PlanBatchLine(margherita.Id, order.Id, "Margherita", 2),
        new PlanBatchLine(diavola.Id, order.Id, "Diavola", 1)
      ],
      Assert.Single(plan.Batches).Lines);
  }

  [Fact]
  public void Build_OpenBatchesKeepTheirIdAndComeBeforePlannedOnes()
  {
    var assigned = CreateOrder("001", -5, Line("Margherita", 4, assigned: 4));
    var waiting = CreateOrder("002", 0, Line("Diavola", 2));
    var open = OpenBatch(-2, (assigned, assigned.Lines[0], 4));

    var plan = CreateBuilder().Build(CreateSnapshot([assigned, waiting], open));

    Assert.Equal(2, plan.Batches.Count);

    Assert.Equal(open.Id, plan.Batches[0].BatchId);
    Assert.Equal(At(-2), plan.Batches[0].EstimatedStartAt);
    Assert.Equal(At(2), plan.Batches[0].EstimatedBakeStartAt);
    Assert.Equal(At(10), plan.Batches[0].EstimatedReadyAt);

    Assert.Null(plan.Batches[1].BatchId);
    Assert.Equal(At(10), plan.Batches[1].EstimatedStartAt);
    Assert.Equal(At(12), plan.Batches[1].EstimatedBakeStartAt);
    Assert.Equal(At(20), plan.Batches[1].EstimatedReadyAt);
  }

  [Fact]
  public void Build_UsesTheInjectedPlanner()
  {
    var a = CreateOrder("001", 0, Line("Margherita", 4));
    var b = CreateOrder("002", 1, Line("Diavola", 4));
    var c = CreateOrder("003", 2, Line("Marinara", 2));
    var snapshot = CreateSnapshot([a, b, c]);

    var fifo = CreateBuilder(new FifoBatchPlanner()).Build(snapshot);
    var backfill = CreateBuilder(new BackfillBatchPlanner(TimeSpan.FromMinutes(2))).Build(snapshot);

    Assert.Equal([4, 6], fifo.Batches.Select(b => b.Lines.Sum(l => l.Quantity)));
    Assert.Equal([6, 4], backfill.Batches.Select(b => b.Lines.Sum(l => l.Quantity)));
  }

  #endregion

  #region Ora stimata degli ordini

  [Fact]
  public void Build_OrderEstimatesFollowTheQueueOrder()
  {
    var a = CreateOrder("001", 0, Line("Margherita", 1));
    var b = CreateOrder("002", 5, Line("Diavola", 1));

    var plan = CreateBuilder().Build(CreateSnapshot([b, a]));

    Assert.Equal(["001", "002"], plan.Orders.Select(o => o.Code));
  }

  [Fact]
  public void Build_OrderIsReadyWhenItsLastBatchIsReady()
  {
    var order = CreateOrder("001", 0, Line("Margherita", 8));

    var plan = CreateBuilder().Build(CreateSnapshot([order]));

    Assert.Equal([At(14), At(24)], plan.Batches.Select(b => b.EstimatedReadyAt));
    Assert.Equal(At(24), ReadyAt(plan, order));
  }

  [Fact]
  public void Build_OrderFullyAssignedKeepsTheEstimateOfItsOpenBatch()
  {
    var order = CreateOrder("001", -5, Line("Margherita", 4, assigned: 4));
    var open = OpenBatch(-2, (order, order.Lines[0], 4));

    var plan = CreateBuilder().Build(CreateSnapshot([order], open));

    Assert.Equal(At(10), ReadyAt(plan, order));
  }

  [Fact]
  public void Build_OrderSpreadOverOpenAndPlannedBatchesUsesTheLastOne()
  {
    var line = Line("Margherita", 6, assigned: 4);
    var order = CreateOrder("001", -5, line);
    var open = OpenBatch(-2, (order, line, 4));

    var plan = CreateBuilder().Build(CreateSnapshot([order], open));

    Assert.Equal([At(10), At(20)], plan.Batches.Select(b => b.EstimatedReadyAt));
    Assert.Equal(At(20), ReadyAt(plan, order));
  }

  [Fact]
  public void Build_OrderEstimatesDependOnThePlanner()
  {
    var a = CreateOrder("001", 0, Line("Margherita", 4));
    var b = CreateOrder("002", 1, Line("Diavola", 4));
    var c = CreateOrder("003", 2, Line("Marinara", 2));
    var snapshot = CreateSnapshot([a, b, c]);

    var fifo = CreateBuilder(new FifoBatchPlanner()).Build(snapshot);
    var backfill = CreateBuilder(new BackfillBatchPlanner(TimeSpan.FromMinutes(2))).Build(snapshot);

    Assert.Equal([At(12), At(26), At(26)], [ReadyAt(fifo, a), ReadyAt(fifo, b), ReadyAt(fifo, c)]);
    Assert.Equal([At(14), At(26), At(14)], [ReadyAt(backfill, a), ReadyAt(backfill, b), ReadyAt(backfill, c)]);
  }

  [Fact]
  public void Build_OrderWithNothingLeftToPlanAndNoOpenBatchHasNoEstimate()
  {
    var order = CreateOrder("001", 0, Line("Margherita", 2, assigned: 2));

    var plan = CreateBuilder().Build(CreateSnapshot([order]));

    Assert.Empty(plan.Batches);
    Assert.Empty(plan.Orders);
  }

  #endregion
}
