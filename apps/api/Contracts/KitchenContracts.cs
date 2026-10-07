namespace Api.Contracts;

public sealed record KitchenBatchLineResponse(string OrderCode, string PizzaName, int Quantity);

public sealed record KitchenBatchResponse(
  Guid? BatchId,
  DateTimeOffset EstimatedStartAt,
  DateTimeOffset EstimatedBakeStartAt,
  DateTimeOffset EstimatedReadyAt,
  IReadOnlyList<KitchenBatchLineResponse> Lines);

public sealed record KitchenViewResponse(IReadOnlyList<KitchenBatchResponse> Batches);
