using Api.Contracts;
using Application.Kitchen;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/kitchen")]
public class KitchenController(IKitchenService kitchen) : ControllerBase
{
  [HttpGet]
  public async Task<KitchenViewResponse> Get(CancellationToken ct) =>
    KitchenViewResponse.From(await kitchen.GetPlanAsync(ct));

  [HttpPost("batches/{id:guid}/take-charge")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> TakeCharge(Guid id, CancellationToken ct)
  {
    if (!await kitchen.TakeChargeAsync(id, ct))
      return NotFound();

    return NoContent();
  }

  [HttpPost("batches/{id:guid}/ready")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> MarkReady(Guid id, CancellationToken ct)
  {
    if (!await kitchen.MarkReadyAsync(id, ct))
      return NotFound();

    return NoContent();
  }
}
