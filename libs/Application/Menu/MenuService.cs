using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Application.Menu;

public sealed class MenuService(AppDbContext db) : IMenuService
{
  public async Task<IReadOnlyList<MenuItem>> GetMenuAsync(CancellationToken ct = default) =>
    await db.Pizzas
      .AsNoTracking()
      .Where(p => p.IsOnMenu)
      .OrderBy(p => p.Name)
      .Select(p => new MenuItem(p.Id, p.Name, p.Description, p.Price))
      .ToListAsync(ct);
}
