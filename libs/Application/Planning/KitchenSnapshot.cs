using System;
using Domain.Kitchen;

namespace Application.Planning;

public sealed record OrderLineSnapshot(Guid Id, string PizzaName, int Quantity, int AssignedQuantity)
{
  public int PendingQuantity => Quantity - AssignedQuantity;
}

public sealed record OrderSnapshot(Guid Id, string Code, DateTimeOffset CreatedAt, IReadOnlyList<OrderLineSnapshot> Lines);

public sealed record BatchLineSnapshot(Guid OrderLineId, Guid OrderId, string PizzaName, int Quantity);

public sealed record BatchSnapshot(Guid Id, DateTimeOffset EarliestStart, IReadOnlyList<BatchLineSnapshot> Lines)
{
  public int PizzaCount => Lines.Sum(l => l.Quantity);
}

public sealed record KitchenSnapshot(
  Baker Baker,
  Oven Oven,
  DateTimeOffset Now,
  IReadOnlyList<BatchSnapshot> OpenBatches,
  IReadOnlyList<OrderSnapshot> ActiveOrders);
