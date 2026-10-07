using Application.Orders;
using Application.Tests.Support;
using Domain.Exceptions;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Orders;

[Collection(DatabaseCollection.Name)]
public class OrderServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
  private readonly DatabaseFixture _fixture = fixture;

  [Fact]
  public async Task Create_ReturnsCodeStatusAndEstimatedTime()
  {
    var tracking = await OrderAsync("Margherita", 2);

    Assert.Equal("001", tracking.Code);
    Assert.Equal(OrderStatus.Queued, tracking.Status);
    Assert.Equal(At(10), tracking.EstimatedReadyAt);
    Assert.Null(tracking.ReadyAt);
  }

  [Fact]
  public async Task Create_PersistsOrderWithPizzaSnapshot()
  {
    await OrderAsync("Diavola", 3);

    await using var db = _fixture.CreateContext();
    var order = await db.Orders.Include(o => o.Lines).SingleAsync();
    var line = Assert.Single(order.Lines);

    Assert.Equal(OrderStatus.Queued, order.Status);
    Assert.Equal(DateOnly.FromDateTime(Noon.DateTime), order.BusinessDate);
    Assert.Equal(Noon, order.CreatedAt);
    Assert.Equal("Diavola", line.PizzaName);
    Assert.Equal(8.00m, line.UnitPrice);
    Assert.Equal(3, line.Quantity);
  }

  [Fact]
  public async Task Create_AssignsSequentialCodesWithinTheDay()
  {
    var first = await OrderAsync("Margherita", 1);
    var second = await OrderAsync("Marinara", 1);
    var third = await OrderAsync("Diavola", 1);

    Assert.Equal(["001", "002", "003"], [first.Code, second.Code, third.Code]);
  }

  [Fact]
  public async Task Create_RestartsCodesOnNewBusinessDay()
  {
    await OrderAsync("Margherita", 1);
    Time.Advance(TimeSpan.FromDays(1));

    var tomorrow = await OrderAsync("Margherita", 1);

    Assert.Equal("001", tomorrow.Code);
  }

  [Fact]
  public async Task Create_WithSimultaneousRequests_AssignsDistinctCodes()
  {
    var margherita = await PizzaIdAsync("Margherita");

    var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(async _ =>
    {
      await using var db = _fixture.CreateContext();

      return await CreateOrderService(db).CreateAsync([new OrderItem(margherita, 1)]);
    }));

    Assert.Equal(["001", "002", "003"], results.Select(r => r.Code).Order());
  }

  [Fact]
  public async Task Create_WithUnknownPizza_ThrowsAndPersistsNothing()
  {
    await Assert.ThrowsAsync<DomainException>(() =>
      CreateOrderService().CreateAsync([new OrderItem(Guid.NewGuid(), 1)]));

    await using var db = _fixture.CreateContext();
    Assert.False(await db.Orders.AnyAsync());
  }

  [Fact]
  public async Task Create_WithPizzaRemovedFromMenu_ThrowsAndPersistsNothing()
  {
    var pizza = await Db.Pizzas.SingleAsync(p => p.Name == "Marinara");
    pizza.RemoveFromMenu();
    await Db.SaveChangesAsync();

    await Assert.ThrowsAsync<DomainException>(() =>
      CreateOrderService().CreateAsync([new OrderItem(pizza.Id, 1)]));

    await using var db = _fixture.CreateContext();
    Assert.False(await db.Orders.AnyAsync());
  }

  [Fact]
  public async Task Create_WithTooManyPizzas_ThrowsAndPersistsNothing()
  {
    await Assert.ThrowsAsync<DomainException>(() => OrderAsync("Margherita", Order.MaxPizzasPerOrder + 1));

    await using var db = _fixture.CreateContext();
    Assert.False(await db.Orders.AnyAsync());
  }

  [Fact]
  public async Task Create_WithNullItems_Throws()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() => CreateOrderService().CreateAsync(null!));
  }

  [Theory]
  [InlineData("abc")]
  [InlineData("")]
  [InlineData("042")]
  public async Task GetByCode_WithUnknownCode_ReturnsNull(string code)
  {
    await OrderAsync("Margherita", 1);

    Assert.Null(await CreateOrderService().GetByCodeAsync(code));
  }

  [Fact]
  public async Task GetByCode_WithQueuedOrder_ReturnsStatusAndEstimatedTime()
  {
    await OrderAsync("Margherita", 2);

    var tracking = await CreateOrderService().GetByCodeAsync("001");

    Assert.NotNull(tracking);
    Assert.Equal("001", tracking.Code);
    Assert.Equal(OrderStatus.Queued, tracking.Status);
    Assert.Equal(At(10), tracking.EstimatedReadyAt);
  }

  [Fact]
  public async Task GetByCode_IgnoresOrdersOfPreviousDays()
  {
    await OrderAsync("Margherita", 1);
    Time.Advance(TimeSpan.FromDays(1));

    Assert.Null(await CreateOrderService().GetByCodeAsync("001"));
  }

  [Fact]
  public async Task GetByCode_WithReadyOrder_ReturnsReadyTimeWithoutEstimate()
  {
    await OrderAsync("Margherita", 2);
    var kitchen = CreateKitchenService();
    var plan = await AssignBatchAsync(kitchen);
    Advance(9);
    await kitchen.MarkReadyAsync(plan.Batches.Single().BatchId!.Value);

    var tracking = await CreateOrderService().GetByCodeAsync("001");

    Assert.NotNull(tracking);
    Assert.Equal(OrderStatus.Ready, tracking.Status);
    Assert.Null(tracking.EstimatedReadyAt);
    Assert.Equal(At(9), tracking.ReadyAt);
  }

  [Fact]
  public async Task GetActive_WithNoOrders_ReturnsEmpty()
  {
    Assert.Empty(await CreateOrderService().GetActiveAsync());
  }

  [Fact]
  public async Task GetActive_ReturnsQueueInArrivalOrderWithStatuses()
  {
    await OrderAsync("Margherita", 2);
    Advance(1);
    await OrderAsync("Marinara", 1);
    await AssignBatchAsync(CreateKitchenService());

    var active = await CreateOrderService().GetActiveAsync();

    Assert.Equal(["001", "002"], active.Select(o => o.Code));
    Assert.All(active, o => Assert.Equal(OrderStatus.InPreparation, o.Status));
    Assert.All(active, o => Assert.NotNull(o.EstimatedReadyAt));
  }

  [Fact]
  public async Task GetActive_ExcludesReadyOrders()
  {
    await OrderAsync("Margherita", 4);
    var kitchen = CreateKitchenService();
    var plan = await AssignBatchAsync(kitchen);
    Advance(20);
    await kitchen.MarkReadyAsync(plan.Batches.Single().BatchId!.Value);
    await OrderAsync("Marinara", 1);

    var active = await CreateOrderService().GetActiveAsync();

    var only = Assert.Single(active);
    Assert.Equal("002", only.Code);
    Assert.Equal(OrderStatus.Queued, only.Status);
  }
}
