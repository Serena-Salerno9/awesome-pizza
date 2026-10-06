using System;

namespace Domain.Kitchen.Planning;

public sealed record PlanningContext(int MaxBatchSize);

public interface IBatchPlanner
{
  IReadOnlyList<PlannedBatch> Plan(IReadOnlyList<PendingOrder> queue, PlanningContext context);
}
