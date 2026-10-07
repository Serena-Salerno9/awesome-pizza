using System;
using Domain.Exceptions;

namespace Domain.Orders;

public static class OrderStateMachine
{
  public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
  {
    (OrderStatus.Queued, OrderStatus.InPreparation) => true,
    (OrderStatus.InPreparation, OrderStatus.Ready) => true,
    _ => false
  };

  public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
  {
    if (!CanTransition(from, to))
      throw new DomainException($"Cannot change order status from {from} to {to}.");
  }
}
