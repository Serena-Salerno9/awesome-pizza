using System;

namespace Domain.Kitchen.Planning;

public sealed class FifoBatchPlanner : IBatchPlanner
{
  public IReadOnlyList<PlannedBatch> Plan(IReadOnlyList<PendingOrder> queue, PlanningContext context)
  {
    ArgumentNullException.ThrowIfNull(queue);
    ArgumentNullException.ThrowIfNull(context);
    ArgumentOutOfRangeException.ThrowIfLessThan(context.MaxBatchSize, 1);

    foreach (var line in queue.SelectMany(o => o.Lines))
      ArgumentOutOfRangeException.ThrowIfLessThan(line.Quantity, 1);

    var batches = new List<PlannedBatch>();
    var current = new List<PlannedBatchLine>();
    var free = context.MaxBatchSize;

    foreach (var order in queue)
    {
      var total = order.Lines.Sum(l => l.Quantity);

      if (total <= context.MaxBatchSize)
      {
        if (total > free)
          CloseCurrent();

        current.AddRange(order.Lines.Select(l => new PlannedBatchLine(l.OrderLineId, l.Quantity)));
        free -= total;
      }
      else
      {
        foreach (var line in order.Lines)
        {
          var remaining = line.Quantity;

          while (remaining > 0)
          {
            if (free == 0)
              CloseCurrent();

            var take = Math.Min(remaining, free);
            current.Add(new PlannedBatchLine(line.OrderLineId, take));
            free -= take;
            remaining -= take;
          }
        }
      }
    }

    CloseCurrent();
    return batches;

    void CloseCurrent()
    {
      if (current.Count != 0)
        batches.Add(new PlannedBatch([.. current]));

      current.Clear();
      free = context.MaxBatchSize;
    }
  }
}
