using Domain.Kitchen;
using Domain.Kitchen.Planning;

namespace Application.Planning;

public sealed class KitchenPlanBuilder
{
  private readonly IBatchPlanner _planner;

  public KitchenPlanBuilder(IBatchPlanner planner)
  {
    ArgumentNullException.ThrowIfNull(planner);

    _planner = planner;
  }

  public KitchenPlan Build(QueueSnapshot snapshot)
  {
    ArgumentNullException.ThrowIfNull(snapshot);

    var openBatches = OrderOpenBatches(snapshot);
    var assigned = openBatches.Select(b => new BatchToEstimate(b.PizzaCount, b.EarliestStart)).ToList();

    var queue = BuildQueue(snapshot);
    var context = new PlanningContext(
      BatchTimeEstimator.MaxBatchSize(snapshot.Baker, snapshot.Oven),
      new KitchenState(snapshot.Baker, snapshot.Oven, snapshot.Now, assigned));
    var planned = _planner.Plan(queue, context);

    var estimates = BatchTimeEstimator.Estimate(
      snapshot.Baker,
      snapshot.Oven,
      assigned.Concat(planned.Select(b => new BatchToEstimate(b.PizzaCount, snapshot.Now))),
      snapshot.Now);

    var lines = snapshot.ActiveOrders
      .SelectMany(o => o.Lines.Select(l => (l.Id, Info: (OrderId: o.Id, l.PizzaName))))
      .ToDictionary(x => x.Id, x => x.Info);

    var assignedBatches = openBatches.Zip(estimates, (batch, estimate) => ToPlanBatch(batch, estimate));
    var plannedBatches = planned.Zip(estimates.Skip(openBatches.Count), (batch, estimate) => ToPlanBatch(batch, estimate, lines));
    var batches = assignedBatches.Concat(plannedBatches).ToList();

    return new KitchenPlan(batches, BuildOrderEstimates(snapshot, batches));
  }

  private static List<BatchSnapshot> OrderOpenBatches(QueueSnapshot snapshot) =>
    [.. snapshot.OpenBatches.OrderBy(b => b.EarliestStart)];

  private static IOrderedEnumerable<OrderSnapshot> InArrivalOrder(QueueSnapshot snapshot) =>
    snapshot.ActiveOrders
      .OrderBy(o => o.CreatedAt)
      .ThenBy(o => o.Code, StringComparer.Ordinal);

  private static List<PendingOrder> BuildQueue(QueueSnapshot snapshot) =>
    [.. InArrivalOrder(snapshot)
      .Select(o => new PendingOrder(
        o.Id,
        [.. o.Lines
          .Where(l => l.PendingQuantity > 0)
          .Select(l => new PendingLine(l.Id, l.PendingQuantity))]))
      .Where(o => o.Lines.Count > 0)];

  private static PlanBatch ToPlanBatch(BatchSnapshot batch, BatchEstimate estimate) =>
    new(
      batch.Id,
      [.. batch.Lines.Select(l => new PlanBatchLine(l.OrderLineId, l.OrderId, l.PizzaName, l.Quantity))],
      estimate.EstimatedStartAt,
      estimate.EstimatedBakeStartAt,
      estimate.EstimatedReadyAt);

  private static PlanBatch ToPlanBatch(
    PlannedBatch batch,
    BatchEstimate estimate,
    Dictionary<Guid, (Guid OrderId, string PizzaName)> lines) =>
    new(
      null,
      [.. batch.Lines.Select(l => new PlanBatchLine(l.OrderLineId, lines[l.OrderLineId].OrderId, lines[l.OrderLineId].PizzaName, l.Quantity))],
      estimate.EstimatedStartAt,
      estimate.EstimatedBakeStartAt,
      estimate.EstimatedReadyAt);

  private static List<OrderEstimate> BuildOrderEstimates(QueueSnapshot snapshot, List<PlanBatch> batches)
  {
    var readyAtByOrder = batches
      .SelectMany(b => b.Lines.Select(l => (l.OrderId, b.EstimatedReadyAt)))
      .GroupBy(x => x.OrderId)
      .ToDictionary(g => g.Key, g => g.Max(x => x.EstimatedReadyAt));

    return [.. InArrivalOrder(snapshot)
      .Where(o => readyAtByOrder.ContainsKey(o.Id))
      .Select(o => new OrderEstimate(o.Id, o.Code, readyAtByOrder[o.Id]))];
  }
}
