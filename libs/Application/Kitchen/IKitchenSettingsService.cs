namespace Application.Kitchen;

public interface IKitchenSettingsService
{
  Task<KitchenSettings> GetAsync(CancellationToken ct = default);

  Task<KitchenSettings> UpdateAsync(KitchenSettings settings, CancellationToken ct = default);
}
