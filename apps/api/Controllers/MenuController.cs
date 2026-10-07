using Api.Contracts;
using Application.Menu;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/menu")]
public class MenuController(IMenuService menu) : ControllerBase
{
  [HttpGet]
  public async Task<IReadOnlyList<MenuItemResponse>> Get(CancellationToken ct) =>
    [.. (await menu.GetMenuAsync(ct)).Select(MenuItemResponse.From)];
}
