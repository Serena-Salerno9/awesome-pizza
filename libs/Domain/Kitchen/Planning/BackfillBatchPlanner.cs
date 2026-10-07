using System;

namespace Domain.Kitchen.Planning;

public sealed class BackfillBatchPlanner : IBatchPlanner
{
  private readonly TimeSpan _tolerance;

  public BackfillBatchPlanner(TimeSpan tolerance)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(tolerance, TimeSpan.Zero);

    _tolerance = tolerance;
  }

  public IReadOnlyList<PlannedBatch> Plan(IReadOnlyList<PendingOrder> queue, PlanningContext context)
  {
    ArgumentNullException.ThrowIfNull(queue);
    ArgumentNullException.ThrowIfNull(context);

    var kitchen = context.Kitchen
      ?? throw new ArgumentException("Kitchen state is required for backfill planning.", nameof(context));

    // Il piano FIFO è il riferimento: nessun ordine può peggiorare di più di T rispetto a questo
    var fifoPlan = new FifoBatchPlanner().Plan(queue, context);
    if (fifoPlan.Count < 2)
      return fifoPlan;

    var orderOfLine = queue
      .SelectMany(o => o.Lines.Select(l => (l.OrderLineId, o.OrderId)))
      .ToDictionary(x => x.OrderLineId, x => x.OrderId);

    var batches = fifoPlan.Select(b => b.Lines.ToList()).ToList();
    var fifoReadyAt = EstimateReadyAt(batches, kitchen, orderOfLine);

    for (var target = 0; target < batches.Count; target++)
    {
      while (TryPullOrderInto(target))
      {
      }
    }

    return batches.Select(b => new PlannedBatch([.. b])).ToList();

    // Anticipa il primo ordine, per ordine di arrivo, che sta intero nei posti liberi
    // dell'infornata e che non fa peggiorare nessuno oltre la tolleranza
    bool TryPullOrderInto(int target)
    {
      var free = context.MaxBatchSize - batches[target].Sum(l => l.Quantity);
      if (free <= 0)
        return false;

      foreach (var order in queue)
      {
        var lineIds = order.Lines.Select(l => l.OrderLineId).ToHashSet();
        var pieces = batches
          .SelectMany((batch, index) => batch.Where(l => lineIds.Contains(l.OrderLineId)).Select(l => (Index: index, Line: l)))
          .ToList();

        // Si anticipano solo ordini interi che stanno in infornate successive
        if (pieces.Count == 0 || pieces.Any(p => p.Index <= target) || pieces.Sum(p => p.Line.Quantity) > free)
          continue;

        var candidate = batches
          .Select(batch => batch.Where(l => !lineIds.Contains(l.OrderLineId)).ToList())
          .ToList();
        candidate[target].AddRange(pieces.Select(p => p.Line));
        candidate.RemoveAll(batch => batch.Count == 0);

        var readyAt = EstimateReadyAt(candidate, kitchen, orderOfLine);
        if (fifoReadyAt.All(r => readyAt[r.Key] <= r.Value + _tolerance))
        {
          batches = candidate;
          return true;
        }
      }

      return false;
    }
  }

  // L'ora stimata di un ordine è l'uscita della sua ultima infornata pianificata
  private static Dictionary<Guid, DateTimeOffset> EstimateReadyAt(
    List<List<PlannedBatchLine>> batches,
    KitchenState kitchen,
    Dictionary<Guid, Guid> orderOfLine)
  {
    var toEstimate = kitchen.AssignedBatches
      .Concat(batches.Select(b => new BatchToEstimate(b.Sum(l => l.Quantity), kitchen.Now)));
    var estimates = BatchTimeEstimator.Estimate(kitchen.Baker, kitchen.Oven, toEstimate, kitchen.Now)
      .Skip(kitchen.AssignedBatches.Count);

    var readyAt = new Dictionary<Guid, DateTimeOffset>();
    foreach (var (batch, estimate) in batches.Zip(estimates))
    {
      foreach (var line in batch)
      {
        var orderId = orderOfLine[line.OrderLineId];
        if (!readyAt.TryGetValue(orderId, out var current) || estimate.EstimatedReadyAt > current)
          readyAt[orderId] = estimate.EstimatedReadyAt;
      }
    }

    return readyAt;
  }
}
