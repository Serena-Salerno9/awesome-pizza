using Domain.Catalog;
using Domain.Kitchen;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DbSeeder
{
  public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
  {
    ArgumentNullException.ThrowIfNull(db);

    if (!await db.Pizzas.AnyAsync(ct))
    {
      db.Pizzas.AddRange(
        new Pizza("Margherita", "Pomodoro, mozzarella, basilico", 6.00m),
        new Pizza("Marinara", "Pomodoro, aglio, origano", 5.00m),
        new Pizza("Diavola", "Pomodoro, mozzarella, salame piccante", 8.00m),
        new Pizza("Quattro formaggi", "Mozzarella, gorgonzola, fontina, parmigiano", 9.00m));
    }

    if (!await db.Workstations.AnyAsync(ct))
    {
      var baker = new Baker("Mario", 4, TimeSpan.FromMinutes(3));
      var oven = new Oven(8, TimeSpan.FromMinutes(4));

      db.Bakers.Add(baker);
      db.Ovens.Add(oven);
      db.Workstations.Add(new Workstation("Postazione 1", baker.Id, oven.Id));
    }

    await db.SaveChangesAsync(ct);
  }
}
