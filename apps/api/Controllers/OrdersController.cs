using Api.Contracts;
using Application.Orders;
using Domain.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
  [HttpPost]
  [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
  [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
  public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
  {
    var tracking = await orders.CreateAsync([.. request.Items.Select(i => new OrderItem(i.PizzaId, i.Quantity))], ct);

    return CreatedAtAction(nameof(GetByCode), new { code = tracking.Code }, OrderResponse.From(tracking));
  }

  [HttpGet("{code}")]
  [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<OrderResponse>> GetByCode(string code, CancellationToken ct)
  {
    var tracking = await orders.GetByCodeAsync(code, ct);
    if (tracking is null)
      return NotFound();

    return OrderResponse.From(tracking);
  }

  [HttpGet("limits")]
  public OrderLimitsResponse GetLimits() => new(Order.MaxPizzasPerOrder);

  [HttpGet]
  public async Task<IReadOnlyList<OrderResponse>> GetActive(CancellationToken ct) =>
    [.. (await orders.GetActiveAsync(ct)).Select(OrderResponse.From)];
}
