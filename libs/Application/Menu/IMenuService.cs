namespace Application.Menu;

public sealed record MenuItem(Guid Id, string Name, string? Description, decimal Price);

public interface IMenuService
{
  Task<IReadOnlyList<MenuItem>> GetMenuAsync(CancellationToken ct = default);
}
