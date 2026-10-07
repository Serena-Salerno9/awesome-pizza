using Application.Planning;
using Domain.Exceptions;
using Domain.Kitchen;
using Domain.Orders;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Kitchen;

public sealed class KitchenService(AppDbContext db, KitchenPlanner planner, TimeProvider time) : IKitchenService
{
  public Task<KitchenPlan> GetPlanAsync(CancellationToken ct = default) =>
    planner.GetPlanAsync(ct);

  public async Task<KitchenPlan> StartNextBatchAsync(CancellationToken ct = default)
  {
    var plan = await planner.GetPlanAsync(ct);
    var next = plan.Batches.FirstOrDefault(b => b.BatchId is null)
      ?? throw new DomainException("There are no batches to start.");

    var now = time.GetUtcNow();
    if (next.EstimatedStartAt > now)
      throw new DomainException("The baker cannot start a new batch yet.");

    var workstation = await db.Workstations.SingleAsync(ct);
    var lineIds = next.Lines.Select(l => l.OrderLineId).ToList();
    var orderLines = await db.OrderLines
      .Where(l => lineIds.Contains(l.Id))
      .Include(l => l.FkOrderNavigation)
      .ToDictionaryAsync(l => l.Id, ct);

    var batch = new Batch(workstation.Id, next.Lines.Select(l => (orderLines[l.OrderLineId], l.Quantity)), now);
    db.Batches.Add(batch);

    var queuedOrders = orderLines.Values
      .Select(l => l.FkOrderNavigation)
      .DistinctBy(o => o.Id)
      .Where(o => o.Status == OrderStatus.Queued);

    foreach (var order in queuedOrders)
      order.StartPreparation(workstation.Id, now);

    await db.SaveChangesAsync(ct);

    return await planner.GetPlanAsync(ct);
  }

  public async Task<bool> TakeChargeAsync(Guid batchId, CancellationToken ct = default)
  {
    var batch = await db.Batches.SingleOrDefaultAsync(b => b.Id == batchId, ct);
    if (batch is null)
      return false;

    batch.TakeCharge(time.GetUtcNow());
    await db.SaveChangesAsync(ct);

    return true;
  }

  public async Task<bool> MarkReadyAsync(Guid batchId, CancellationToken ct = default)
  {
    var batch = await db.Batches
      .Include(b => b.Lines).ThenInclude(l => l.FkOrderLineNavigation)
      .SingleOrDefaultAsync(b => b.Id == batchId, ct);
    if (batch is null)
      return false;

    var now = time.GetUtcNow();
    batch.MarkReady(now);

    var orderIds = batch.Lines.Select(l => l.FkOrderLineNavigation.FkOrder).Distinct().ToList();
    var orders = await db.Orders
      .Where(o => orderIds.Contains(o.Id))
      .Include(o => o.Lines)
      .ToListAsync(ct);

    var lineIds = orders.SelectMany(o => o.Lines).Select(l => l.Id).ToList();
    var batchLines = await db.BatchLines
      .Where(bl => lineIds.Contains(bl.FkOrderLine))
      .Include(bl => bl.FkBatchNavigation)
      .ToListAsync(ct);

    foreach (var order in orders.Where(o => IsComplete(o, batchLines)))
      order.MarkReady(now);

    await db.SaveChangesAsync(ct);

    return true;
  }

  private static bool IsComplete(Order order, List<BatchLine> batchLines)
  {
    var orderBatchLines = batchLines.Where(bl => order.Lines.Any(l => l.Id == bl.FkOrderLine)).ToList();

    return order.Lines.All(l => orderBatchLines.Where(bl => bl.FkOrderLine == l.Id).Sum(bl => bl.Quantity) == l.Quantity)
      && orderBatchLines.All(bl => bl.FkBatchNavigation.ReadyAt is not null);
  }
}
