using Application.Kitchen;
using Application.Tests.Support;
using Domain.Exceptions;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Kitchen;

[Collection(DatabaseCollection.Name)]
public class KitchenSettingsServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
  private readonly DatabaseFixture _fixture = fixture;

  private static readonly KitchenSettings Expert =
    new(12, TimeSpan.FromMinutes(1), 8, TimeSpan.FromMinutes(4));

  public static TheoryData<int, double, int, double> InvalidSettings => new()
  {
    { 0, 1, 8, 4 },
    { 12, 0, 8, 4 },
    { 12, 1, 0, 4 },
    { 12, 1, 8, 0 },
  };

  [Fact]
  public async Task Get_ReturnsSeededSettings()
  {
    var settings = await CreateKitchenSettingsService().GetAsync();

    Assert.Equal(new KitchenSettings(4, TimeSpan.FromMinutes(3), 8, TimeSpan.FromMinutes(4)), settings);
  }

  [Fact]
  public async Task Update_PersistsNewValuesOnTheSameWorkstation()
  {
    var updated = new KitchenSettings(12, TimeSpan.FromSeconds(90), 6, TimeSpan.FromMinutes(8));

    var result = await CreateKitchenSettingsService().UpdateAsync(updated);

    await using var db = _fixture.CreateContext();
    Assert.Equal(updated, result);
    Assert.Equal(updated, await CreateKitchenSettingsService(db).GetAsync());
    Assert.Equal(1, await db.Bakers.CountAsync());
    Assert.Equal(1, await db.Ovens.CountAsync());
    Assert.Equal(1, await db.Workstations.CountAsync());
  }

  [Fact]
  public async Task Update_ChangesEstimatedTimeOfQueuedOrders()
  {
    await OrderAsync("Margherita", 6);
    var orders = CreateOrderService();
    Assert.Equal(At(26), (await orders.GetByCodeAsync("001"))!.EstimatedReadyAt);

    await CreateKitchenSettingsService().UpdateAsync(Expert);

    Assert.Equal(At(10), (await orders.GetByCodeAsync("001"))!.EstimatedReadyAt);
  }

  [Fact]
  public async Task Update_WithBatchesInProgress_ThrowsAndKeepsValues()
  {
    await OrderAsync("Margherita", 2);
    await AssignBatchAsync(CreateKitchenService());

    await Assert.ThrowsAsync<DomainException>(() => CreateKitchenSettingsService().UpdateAsync(Expert));

    await using var db = _fixture.CreateContext();
    Assert.Equal(4, (await CreateKitchenSettingsService(db).GetAsync()).MaxConcurrentPizzas);
  }

  [Fact]
  public async Task Update_AfterBatchesAreReady_Succeeds()
  {
    await OrderAsync("Margherita", 2);
    var kitchen = CreateKitchenService();
    var plan = await AssignBatchAsync(kitchen);
    Advance(10);
    await kitchen.MarkReadyAsync(plan.Batches.Single().BatchId!.Value);

    var result = await CreateKitchenSettingsService().UpdateAsync(Expert);

    Assert.Equal(Expert, result);
    await using var db = _fixture.CreateContext();
    Assert.Equal(OrderStatus.Ready, (await db.Orders.SingleAsync()).Status);
  }

  [Theory]
  [MemberData(nameof(InvalidSettings))]
  public async Task Update_WithInvalidValues_ThrowsAndKeepsValues(
    int maxConcurrentPizzas,
    double preparationMinutes,
    int ovenCapacity,
    double bakingMinutes)
  {
    var invalid = new KitchenSettings(
      maxConcurrentPizzas,
      TimeSpan.FromMinutes(preparationMinutes),
      ovenCapacity,
      TimeSpan.FromMinutes(bakingMinutes));

    await Assert.ThrowsAsync<DomainException>(() => CreateKitchenSettingsService().UpdateAsync(invalid));

    await using var db = _fixture.CreateContext();
    Assert.Equal(
      new KitchenSettings(4, TimeSpan.FromMinutes(3), 8, TimeSpan.FromMinutes(4)),
      await CreateKitchenSettingsService(db).GetAsync());
  }

  [Fact]
  public async Task Update_WithNullSettings_Throws()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() => CreateKitchenSettingsService().UpdateAsync(null!));
  }
}
