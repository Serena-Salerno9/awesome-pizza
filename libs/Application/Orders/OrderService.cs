using Application.Planning;
using Domain.Exceptions;
using Domain.Orders;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders;

public sealed class OrderService(AppDbContext db, KitchenPlanner planner, TimeProvider time) : IOrderService
{
  private const int MaxCreateAttempts = 3;

  public async Task<OrderTracking> CreateAsync(IReadOnlyList<OrderItem> items, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(items);

    var pizzaIds = items.Select(i => i.PizzaId).Distinct().ToList();
    var pizzas = await db.Pizzas.Where(p => pizzaIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

    var lines = items
      .Select(i => pizzas.TryGetValue(i.PizzaId, out var pizza)
        ? (Pizza: pizza, i.Quantity)
        : throw new DomainException($"Pizza '{i.PizzaId}' does not exist."))
      .ToList();

    for (var attempt = 1; ; attempt++)
    {
      var businessDate = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
      var lastNumber = await db.Orders
        .Where(o => o.BusinessDate == businessDate)
        .MaxAsync(o => (int?)o.DailyNumber, ct) ?? 0;

      var order = new Order(businessDate, lastNumber + 1, lines, time.GetUtcNow());
      db.Orders.Add(order);

      try
      {
        await db.SaveChangesAsync(ct);
      }
      catch (DbUpdateException) when (attempt < MaxCreateAttempts)
      {

        db.ChangeTracker.Clear();
        continue;
      }

      var plan = await planner.GetPlanAsync(ct);
      var estimate = plan.Orders.Single(o => o.OrderId == order.Id);

      return new OrderTracking(order.Code, order.Status, estimate.EstimatedReadyAt, null);
    }
  }

  public async Task<OrderTracking?> GetByCodeAsync(string code, CancellationToken ct = default)
  {
    if (!int.TryParse(code, out var dailyNumber))
      return null;

    var businessDate = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
    var order = await db.Orders
      .AsNoTracking()
      .SingleOrDefaultAsync(o => o.BusinessDate == businessDate && o.DailyNumber == dailyNumber, ct);

    if (order is null)
      return null;

    if (order.Status == OrderStatus.Ready)
      return new OrderTracking(order.Code, order.Status, null, order.ReadyAt);

    var plan = await planner.GetPlanAsync(ct);

    return new OrderTracking(order.Code, order.Status, plan.Orders.FirstOrDefault(o => o.OrderId == order.Id)?.EstimatedReadyAt, null);
  }

  public async Task<IReadOnlyList<OrderTracking>> GetActiveAsync(CancellationToken ct = default)
  {
    var plan = await planner.GetPlanAsync(ct);
    var ids = plan.Orders.Select(o => o.OrderId).ToList();
    var statuses = await db.Orders
      .AsNoTracking()
      .Where(o => ids.Contains(o.Id))
      .ToDictionaryAsync(o => o.Id, o => o.Status, ct);

    return [.. plan.Orders.Select(o => new OrderTracking(o.Code, statuses[o.OrderId], o.EstimatedReadyAt, null))];
  }
}
