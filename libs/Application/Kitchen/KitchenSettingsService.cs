using Domain.Exceptions;
using Domain.Kitchen;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Kitchen;

public sealed class KitchenSettingsService(AppDbContext db) : IKitchenSettingsService
{
  public async Task<KitchenSettings> GetAsync(CancellationToken ct = default) =>
    ToSettings(await LoadWorkstationAsync(ct));

  public async Task<KitchenSettings> UpdateAsync(KitchenSettings settings, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var workstation = await LoadWorkstationAsync(ct);

    var hasOpenBatches = await db.Batches.AnyAsync(b => b.FkWorkstation == workstation.Id && b.ReadyAt == null, ct);
    if (hasOpenBatches)
      throw new DomainException("Kitchen settings cannot be changed while batches are in progress.");

    workstation.FkBakerNavigation.Update(settings.MaxConcurrentPizzas, settings.PreparationTimePerPizza);
    workstation.FkOvenNavigation.Update(settings.OvenCapacity, settings.BakingTime);

    await db.SaveChangesAsync(ct);

    return ToSettings(workstation);
  }

  private Task<Workstation> LoadWorkstationAsync(CancellationToken ct) =>
    db.Workstations
      .Include(w => w.FkBakerNavigation)
      .Include(w => w.FkOvenNavigation)
      .SingleAsync(ct);

  private static KitchenSettings ToSettings(Workstation workstation) =>
    new(
      workstation.FkBakerNavigation.MaxConcurrentPizzas,
      workstation.FkBakerNavigation.PreparationTimePerPizza,
      workstation.FkOvenNavigation.Capacity,
      workstation.FkOvenNavigation.BakingTime);
}
