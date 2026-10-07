using System;

namespace Domain.Kitchen.Planning;

public sealed record PlannedBatchLine(Guid OrderLineId, int Quantity);

public sealed record PlannedBatch(IReadOnlyList<PlannedBatchLine> Lines)
{
  public int PizzaCount => Lines.Sum(l => l.Quantity);
}
