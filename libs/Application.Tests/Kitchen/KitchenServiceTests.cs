using Application.Tests.Support;
using Domain.Exceptions;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Kitchen;

[Collection(DatabaseCollection.Name)]
public class KitchenServiceTests(DatabaseFixture fixture) : DatabaseTestBase(fixture)
{
  private readonly DatabaseFixture _fixture = fixture;

  [Fact]
  public async Task GetPlan_WithNoOrders_ReturnsEmptyPlan()
  {
    var plan = await CreateKitchenService().GetPlanAsync();

    Assert.Empty(plan.Batches);
    Assert.Empty(plan.Orders);
  }

  [Fact]
  public async Task GetPlan_ProposesFirstBatchWithPizzasToPrepare()
  {
    await OrderAsync("Margherita", 2);
    await OrderAsync("Diavola", 1);

    var plan = await CreateKitchenService().GetPlanAsync();

    var batch = Assert.Single(plan.Batches);
    Assert.Null(batch.BatchId);
    Assert.Equal(3, batch.Lines.Sum(l => l.Quantity));
    Assert.Equal(["Margherita", "Diavola"], batch.Lines.Select(l => l.PizzaName));
    Assert.Equal(At(0), batch.EstimatedStartAt);
  }

  [Fact]
  public async Task GetPlan_SplitsOrderLargerThanMaximumBatch()
  {
    await OrderAsync("Margherita", 6);

    var plan = await CreateKitchenService().GetPlanAsync();

    Assert.Equal([4, 2], plan.Batches.Select(b => b.Lines.Sum(l => l.Quantity)));
    Assert.Single(plan.Orders);
  }

  [Fact]
  public async Task AssignDueBatches_WithNoOrders_AssignsNothing()
  {
    Assert.Equal(0, await CreateKitchenService().AssignDueBatchesAsync());

    await using var db = _fixture.CreateContext();
    Assert.False(await db.Batches.AnyAsync());
  }

  [Fact]
  public async Task AssignDueBatches_PersistsBatchAndStartsPreparationOfItsOrders()
  {
    await OrderAsync("Margherita", 2);
    await OrderAsync("Marinara", 1);

    var plan = await AssignBatchAsync(CreateKitchenService());

    await using var db = _fixture.CreateContext();
    var batch = await db.Batches.Include(b => b.Lines).SingleAsync();
    var workstation = await db.Workstations.SingleAsync();
    var orders = await db.Orders.OrderBy(o => o.DailyNumber).ToListAsync();

    Assert.Equal(Noon, batch.StartedAt);
    Assert.Null(batch.TakenAt);
    Assert.Null(batch.ReadyAt);
    Assert.Equal(3, batch.Lines.Sum(l => l.Quantity));
    Assert.All(orders, o => Assert.Equal(OrderStatus.InPreparation, o.Status));
    Assert.All(orders, o => Assert.Equal(workstation.Id, o.FkWorkstation));
    Assert.All(orders, o => Assert.Equal(Noon, o.StartedAt));
    Assert.Equal(batch.Id, Assert.Single(plan.Batches).BatchId);
  }

  [Fact]
  public async Task AssignDueBatches_WithLargeOrder_AssignsOnlyFirstBatchAndKeepsRestInPlan()
  {
    await OrderAsync("Margherita", 6);

    var plan = await AssignBatchAsync(CreateKitchenService());

    Assert.Equal([4, 2], plan.Batches.Select(b => b.Lines.Sum(l => l.Quantity)));
    Assert.NotNull(plan.Batches[0].BatchId);
    Assert.Null(plan.Batches[1].BatchId);

    await using var db = _fixture.CreateContext();
    Assert.Equal(OrderStatus.InPreparation, (await db.Orders.SingleAsync()).Status);
    Assert.Equal(4, await db.BatchLines.SumAsync(bl => bl.Quantity));
  }

  [Fact]
  public async Task AssignDueBatches_WhenBakerCannotStartYet_AssignsNothing()
  {
    await OrderAsync("Margherita", 4);
    await OrderAsync("Marinara", 1);
    var kitchen = CreateKitchenService();
    await AssignBatchAsync(kitchen);

    Assert.Equal(0, await kitchen.AssignDueBatchesAsync());

    await using var db = _fixture.CreateContext();
    Assert.Equal(1, await db.Batches.CountAsync());
    Assert.Equal(OrderStatus.Queued, (await db.Orders.SingleAsync(o => o.DailyNumber == 2)).Status);
  }

  [Fact]
  public async Task AssignDueBatches_AfterPreviousBatchIsReady_AssignsNextOne()
  {
    await OrderAsync("Margherita", 4);
    await OrderAsync("Marinara", 1);
    var kitchen = CreateKitchenService();
    var first = await AssignBatchAsync(kitchen);
    Advance(16);
    await kitchen.MarkReadyAsync(first.Batches[0].BatchId!.Value);

    var plan = await AssignBatchAsync(kitchen);

    Assert.NotNull(Assert.Single(plan.Batches).BatchId);
    await using var db = _fixture.CreateContext();
    Assert.Equal(2, await db.Batches.CountAsync());
    Assert.Equal(OrderStatus.InPreparation, (await db.Orders.SingleAsync(o => o.DailyNumber == 2)).Status);
  }

  [Fact]
  public async Task TakeCharge_WithUnknownBatch_ReturnsFalse()
  {
    Assert.False(await CreateKitchenService().TakeChargeAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task TakeCharge_RecordsTimeOnBatch()
  {
    await OrderAsync("Margherita", 2);
    var kitchen = CreateKitchenService();
    var plan = await AssignBatchAsync(kitchen);
    Advance(2);

    Assert.True(await kitchen.TakeChargeAsync(plan.Batches.Single().BatchId!.Value));

    await using var db = _fixture.CreateContext();
    Assert.Equal(At(2), (await db.Batches.SingleAsync()).TakenAt);
  }

  [Fact]
  public async Task TakeCharge_Twice_Throws()
  {
    await OrderAsync("Margherita", 2);
    var kitchen = CreateKitchenService();
    var batchId = (await AssignBatchAsync(kitchen)).Batches.Single().BatchId!.Value;
    await kitchen.TakeChargeAsync(batchId);

    await Assert.ThrowsAsync<DomainException>(() => kitchen.TakeChargeAsync(batchId));
  }

  [Fact]
  public async Task MarkReady_WithUnknownBatch_ReturnsFalse()
  {
    Assert.False(await CreateKitchenService().MarkReadyAsync(Guid.NewGuid()));
  }

  [Fact]
  public async Task MarkReady_MarksOrderReadyWhenAllItsPizzasAreOut()
  {
    await OrderAsync("Margherita", 2);
    var kitchen = CreateKitchenService();
    var batchId = (await AssignBatchAsync(kitchen)).Batches.Single().BatchId!.Value;
    Advance(10);

    Assert.True(await kitchen.MarkReadyAsync(batchId));

    await using var db = _fixture.CreateContext();
    var order = await db.Orders.SingleAsync();
    Assert.Equal(OrderStatus.Ready, order.Status);
    Assert.Equal(At(10), order.ReadyAt);
    Assert.Equal(At(10), (await db.Batches.SingleAsync()).ReadyAt);
  }

  [Fact]
  public async Task MarkReady_WithSharedBatch_MarksEveryOrderOfTheBatchReady()
  {
    await OrderAsync("Margherita", 2);
    await OrderAsync("Marinara", 1);
    var kitchen = CreateKitchenService();
    var batchId = (await AssignBatchAsync(kitchen)).Batches.Single().BatchId!.Value;
    Advance(10);

    await kitchen.MarkReadyAsync(batchId);

    await using var db = _fixture.CreateContext();
    Assert.All(await db.Orders.ToListAsync(), o => Assert.Equal(OrderStatus.Ready, o.Status));
  }

  [Fact]
  public async Task MarkReady_WithSplitOrder_MarksItReadyOnlyAfterTheLastBatch()
  {
    await OrderAsync("Margherita", 6);
    var kitchen = CreateKitchenService();
    var firstId = (await AssignBatchAsync(kitchen)).Batches[0].BatchId!.Value;
    Advance(16);

    await kitchen.MarkReadyAsync(firstId);

    await using (var db = _fixture.CreateContext())
      Assert.Equal(OrderStatus.InPreparation, (await db.Orders.SingleAsync()).Status);

    var secondId = (await AssignBatchAsync(kitchen)).Batches.Single().BatchId!.Value;
    Advance(10);
    await kitchen.MarkReadyAsync(secondId);

    await using (var db = _fixture.CreateContext())
    {
      var order = await db.Orders.SingleAsync();
      Assert.Equal(OrderStatus.Ready, order.Status);
      Assert.Equal(At(26), order.ReadyAt);
    }
  }

  [Fact]
  public async Task MarkReady_Twice_Throws()
  {
    await OrderAsync("Margherita", 2);
    var kitchen = CreateKitchenService();
    var batchId = (await AssignBatchAsync(kitchen)).Batches.Single().BatchId!.Value;
    await kitchen.MarkReadyAsync(batchId);

    await Assert.ThrowsAsync<DomainException>(() => kitchen.MarkReadyAsync(batchId));
  }
}
