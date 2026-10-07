using Api.Contracts;
using Api.Workers;
using Application.Kitchen;
using Application.Planning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/kitchen/settings")]
public class KitchenSettingsController(
  IKitchenSettingsService settings,
  PlanningOptions planning,
  IOptions<BatchAssignmentOptions> assignment) : ControllerBase
{
  [HttpGet]
  public async Task<KitchenSettingsResponse> Get(CancellationToken ct) =>
    KitchenSettingsResponse.From(await settings.GetAsync(ct), planning, assignment.Value);

  [HttpPut]
  [ProducesResponseType<KitchenSettingsResponse>(StatusCodes.Status200OK)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
  public async Task<KitchenSettingsResponse> Update(KitchenSettingsRequest request, CancellationToken ct) =>
    KitchenSettingsResponse.From(await settings.UpdateAsync(request.ToSettings(), ct), planning, assignment.Value);
}
