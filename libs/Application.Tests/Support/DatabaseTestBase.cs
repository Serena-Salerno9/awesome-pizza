using Application.Kitchen;
using Application.Orders;
using Application.Planning;
using Domain.Kitchen.Planning;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Application.Tests.Support;

public abstract class DatabaseTestBase(DatabaseFixture fixture) : IAsyncLifetime
{
  protected static readonly DateTimeOffset Noon = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

  protected FakeTimeProvider Time { get; } = new(Noon);

  protected AppDbContext Db { get; private set; } = null!;

  public async Task InitializeAsync()
  {
    await fixture.ResetAsync();
    Db = fixture.CreateContext();
  }

  public async Task DisposeAsync() => await Db.DisposeAsync();

  protected AppDbContext NewContext() => fixture.CreateContext();

  protected OrderService CreateOrderService(AppDbContext? db = null) =>
    new(db ?? Db, CreatePlanner(db ?? Db), Time);

  protected KitchenService CreateKitchenService(AppDbContext? db = null) =>
    new(db ?? Db, CreatePlanner(db ?? Db), Time);

  protected Task<Guid> PizzaIdAsync(string name) =>
    Db.Pizzas.Where(p => p.Name == name).Select(p => p.Id).SingleAsync();

  protected async Task<OrderTracking> OrderAsync(string pizzaName, int quantity) =>
    await CreateOrderService().CreateAsync([new OrderItem(await PizzaIdAsync(pizzaName), quantity)]);

  protected void Advance(int minutes) => Time.Advance(TimeSpan.FromMinutes(minutes));

  protected DateTimeOffset At(int minutes) => Noon.AddMinutes(minutes);

  private KitchenPlanner CreatePlanner(AppDbContext db) =>
    new(db, new KitchenPlanBuilder(new FifoBatchPlanner()), Time);
}
