using Application.Menu;

namespace Api.Contracts;

public sealed record MenuItemResponse(Guid Id, string Name, string? Description, decimal Price)
{
  public static MenuItemResponse From(MenuItem item) =>
    new(item.Id, item.Name, item.Description, item.Price);
}
