using System;
using Domain.Orders;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Planning;

public sealed class QueuePlanner(AppDbContext db, KitchenPlanBuilder builder, TimeProvider time)
{
  public async Task<KitchenPlan> GetPlanAsync(CancellationToken ct = default)
  {
    var snapshot = await LoadSnapshotAsync(ct);

    return builder.Build(snapshot);
  }

  private async Task<QueueSnapshot> LoadSnapshotAsync(CancellationToken ct)
  {
    var workstation = await db.Workstations
      .Include(w => w.FkBakerNavigation)
      .Include(w => w.FkOvenNavigation)
      .AsNoTracking()
      .SingleAsync(ct);

    var openBatches = await db.Batches
      .Where(b => b.FkWorkstation == workstation.Id && b.ReadyAt == null)
      .Include(b => b.Lines).ThenInclude(l => l.FkOrderLineNavigation)
      .AsNoTracking()
      .ToListAsync(ct);

    var orders = await db.Orders
      .Where(o => o.Status != OrderStatus.Ready)
      .Include(o => o.Lines)
      .AsNoTracking()
      .ToListAsync(ct);

    var assigned = await db.BatchLines
      .Where(bl => bl.FkOrderLineNavigation.FkOrderNavigation.Status != OrderStatus.Ready)
      .GroupBy(bl => bl.FkOrderLine)
      .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
      .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);

    return new QueueSnapshot(
      workstation.FkBakerNavigation,
      workstation.FkOvenNavigation,
      time.GetUtcNow(),
      [.. openBatches.Select(b => new BatchSnapshot(
        b.Id,
        b.TakenAt ?? b.StartedAt,
        [.. b.Lines.Select(l => new BatchLineSnapshot(
          l.FkOrderLine,
          l.FkOrderLineNavigation.FkOrder,
          l.FkOrderLineNavigation.PizzaName,
          l.Quantity))]))],
      [.. orders.Select(o => new OrderSnapshot(
        o.Id,
        o.Code,
        o.CreatedAt,
        [.. o.Lines.Select(l => new OrderLineSnapshot(l.Id, l.PizzaName, l.Quantity, assigned.GetValueOrDefault(l.Id)))]))]);
  }
}
