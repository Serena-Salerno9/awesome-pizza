using System.ComponentModel.DataAnnotations;
using Application.Orders;
using Domain.Orders;

namespace Api.Contracts;

public sealed record OrderItemRequest(
  [Required] Guid PizzaId,
  [Range(1, Order.MaxPizzasPerOrder)] int Quantity);

public sealed record CreateOrderRequest(
  [Required, MinLength(1)] IReadOnlyList<OrderItemRequest> Items);

public sealed record OrderResponse(string Code, OrderStatus Status, DateTimeOffset? EstimatedReadyAt, DateTimeOffset? ReadyAt)
{
  public static OrderResponse From(OrderTracking tracking) =>
    new(tracking.Code, tracking.Status, tracking.EstimatedReadyAt, tracking.ReadyAt);
}

public sealed record OrderLimitsResponse(int MaxPizzasPerOrder);
