using Application.Planning;

namespace Api.Contracts;

public sealed record KitchenBatchLineResponse(string OrderCode, string PizzaName, int Quantity);

public sealed record KitchenBatchResponse(
  Guid? BatchId,
  DateTimeOffset EstimatedStartAt,
  DateTimeOffset EstimatedBakeStartAt,
  DateTimeOffset EstimatedReadyAt,
  IReadOnlyList<KitchenBatchLineResponse> Lines);

public sealed record KitchenViewResponse(IReadOnlyList<KitchenBatchResponse> Batches)
{
  public static KitchenViewResponse From(KitchenPlan plan)
  {
    var codes = plan.Orders.ToDictionary(o => o.OrderId, o => o.Code);

    return new([.. plan.Batches.Select(b => new KitchenBatchResponse(
      b.BatchId,
      b.EstimatedStartAt,
      b.EstimatedBakeStartAt,
      b.EstimatedReadyAt,
      [.. b.Lines.Select(l => new KitchenBatchLineResponse(codes[l.OrderId], l.PizzaName, l.Quantity))]))]);
  }
}
