using System;

namespace Domain.Kitchen.Planning;

public sealed record KitchenState(
  Baker Baker,
  Oven Oven,
  DateTimeOffset Now,
  IReadOnlyList<BatchToEstimate> AssignedBatches);

public sealed record PlanningContext(int MaxBatchSize, KitchenState? Kitchen = null);

public interface IBatchPlanner
{
  IReadOnlyList<PlannedBatch> Plan(IReadOnlyList<PendingOrder> queue, PlanningContext context);
}
