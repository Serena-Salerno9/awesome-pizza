using System;
using Domain.Orders;

namespace Application.Orders;

public sealed record OrderItem(Guid PizzaId, int Quantity);

public sealed record OrderTracking(string Code, OrderStatus Status, DateTimeOffset? EstimatedReadyAt, DateTimeOffset? ReadyAt);

public interface IOrderService
{
  Task<OrderTracking> CreateAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default);

  Task<OrderTracking?> GetByCodeAsync(string code, CancellationToken ct = default);

  Task<IReadOnlyList<OrderTracking>> GetActiveAsync(CancellationToken ct = default);
}
