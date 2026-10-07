using Application.Planning;

namespace Application.Kitchen;

public interface IKitchenService
{
  Task<KitchenPlan> GetPlanAsync(CancellationToken ct = default);

  Task<KitchenPlan> StartNextBatchAsync(CancellationToken ct = default);

  Task<bool> TakeChargeAsync(Guid batchId, CancellationToken ct = default);

  Task<bool> MarkReadyAsync(Guid batchId, CancellationToken ct = default);
}
