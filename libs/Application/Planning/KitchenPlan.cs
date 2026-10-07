using System;

namespace Application.Planning;

public sealed record PlanBatch(
  Guid? BatchId,
  IReadOnlyList<PlanBatchLine> Lines,
  DateTimeOffset EstimatedStartAt,
  DateTimeOffset EstimatedBakeStartAt,
  DateTimeOffset EstimatedReadyAt);

public sealed record PlanBatchLine(Guid OrderLineId, Guid OrderId, string PizzaName, int Quantity);

public sealed record OrderEstimate(Guid OrderId, string Code, DateTimeOffset EstimatedReadyAt);

public sealed record KitchenPlan(
  IReadOnlyList<PlanBatch> Batches,
  IReadOnlyList<OrderEstimate> Orders);
