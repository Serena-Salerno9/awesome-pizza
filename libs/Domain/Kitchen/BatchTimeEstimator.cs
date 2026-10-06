using System;

namespace Domain.Kitchen;

public sealed record BatchEstimate(
  DateTimeOffset EstimatedStartAt,
  DateTimeOffset EstimatedBakeStartAt,
  DateTimeOffset EstimatedReadyAt);

public sealed record BatchToEstimate(int PizzaCount, DateTimeOffset EarliestStart);

public static class BatchTimeEstimator
{
  public static int MaxBatchSize(Baker baker, Oven oven) =>
    Math.Min(baker.MaxConcurrentPizzas, oven.Capacity);

  public static IReadOnlyList<BatchEstimate> Estimate(
    Baker baker,
    Oven oven,
    IEnumerable<BatchToEstimate> batches,
    DateTimeOffset now)
  {
    ArgumentNullException.ThrowIfNull(baker);
    ArgumentNullException.ThrowIfNull(oven);
    ArgumentNullException.ThrowIfNull(batches);

    var batchList = batches.ToList();
    if (batchList.Count == 0)
      return [];

    var maxBatchSize = MaxBatchSize(baker, oven);
    var kitchenFreeAt = batchList.Min(b => b.EarliestStart);
    var bakerFreeAt = kitchenFreeAt;
    var openPizzaFreeAt = Enumerable.Repeat(kitchenFreeAt, baker.MaxConcurrentPizzas).ToArray();
    var ovenPostFreeAt = Enumerable.Repeat(kitchenFreeAt, oven.Capacity).ToArray();
    var estimates = new List<BatchEstimate>();

    foreach (var (pizzaCount, earliestStart) in batchList)
    {
      ArgumentOutOfRangeException.ThrowIfLessThan(pizzaCount, 1);
      ArgumentOutOfRangeException.ThrowIfGreaterThan(pizzaCount, maxBatchSize);

      DateTimeOffset? startAt = null;
      var batchPizzaIndexes = new List<int>();

      for (var i = 0; i < pizzaCount; i++)
      {
        var pizzaIndex = IndexesOfEarliest(openPizzaFreeAt, 1)[0];
        var prepStart = Max(Max(bakerFreeAt, openPizzaFreeAt[pizzaIndex]), earliestStart);
        bakerFreeAt = prepStart + baker.PreparationTimePerPizza;

        openPizzaFreeAt[pizzaIndex] = DateTimeOffset.MaxValue;
        batchPizzaIndexes.Add(pizzaIndex);
        startAt ??= prepStart;
      }

      var batchPostIndexes = IndexesOfEarliest(ovenPostFreeAt, pizzaCount);
      var bakeStart = Max(bakerFreeAt, batchPostIndexes.Max(p => ovenPostFreeAt[p]));
      var readyAt = bakeStart + oven.BakingTime;

      if (readyAt < now)
        readyAt = now;

      foreach (var pizzaIndex in batchPizzaIndexes)
        openPizzaFreeAt[pizzaIndex] = readyAt;

      foreach (var postIndex in batchPostIndexes)
        ovenPostFreeAt[postIndex] = readyAt;

      estimates.Add(new BatchEstimate(startAt!.Value, bakeStart, readyAt));
    }

    return estimates;
  }

  private static int[] IndexesOfEarliest(DateTimeOffset[] freeAt, int count) =>
    Enumerable.Range(0, freeAt.Length).OrderBy(i => freeAt[i]).Take(count).ToArray();

  private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
