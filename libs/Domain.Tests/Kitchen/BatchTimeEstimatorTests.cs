using Domain.Kitchen;

namespace Domain.Tests.Kitchen;

public class BatchTimeEstimatorTests
{
  private static readonly DateTimeOffset Noon = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

  private static DateTimeOffset At(int minutes) => Noon.AddMinutes(minutes);

  private static Baker CreateBaker(int preparationMinutes = 2, int maxConcurrentPizzas = 4) =>
    new("Mario", maxConcurrentPizzas, TimeSpan.FromMinutes(preparationMinutes));

  private static Oven CreateOven(int capacity = 8, int bakingMinutes = 5) =>
    new(capacity, TimeSpan.FromMinutes(bakingMinutes));

  private static IReadOnlyList<BatchEstimate> Estimate(
    Baker baker,
    Oven oven,
    int now,
    params (int PizzaCount, int EarliestStart)[] batches) =>
    BatchTimeEstimator.Estimate(baker, oven, batches.Select(b => (b.PizzaCount, At(b.EarliestStart))), At(now));

  #region Validazione

  [Fact]
  public void Estimate_WithNoBatches_ReturnsEmpty()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 0);

    Assert.Empty(result);
  }

  [Fact]
  public void Estimate_WithNullBatches_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => BatchTimeEstimator.Estimate(CreateBaker(), CreateOven(), null!, Noon));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Estimate_WithPizzaCountBelowOne_Throws(int pizzaCount)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Estimate(CreateBaker(), CreateOven(), now: 0, (pizzaCount, 0)));
  }

  [Fact]
  public void Estimate_WithBatchAboveMaxSize_Throws()
  {
    var baker = CreateBaker(maxConcurrentPizzas: 4);
    var oven = CreateOven(capacity: 8);

    Assert.Throws<ArgumentOutOfRangeException>(() => Estimate(baker, oven, now: 0, (5, 0)));
  }

  [Theory]
  [InlineData(4, 8, 4)]
  [InlineData(12, 8, 8)]
  [InlineData(6, 6, 6)]
  public void MaxBatchSize_IsMinOfOpenPizzasAndOvenCapacity(int maxConcurrentPizzas, int capacity, int expected)
  {
    var size = BatchTimeEstimator.MaxBatchSize(CreateBaker(maxConcurrentPizzas: maxConcurrentPizzas), CreateOven(capacity: capacity));

    Assert.Equal(expected, size);
  }

  #endregion

  #region Infornata singola

  [Fact]
  public void Estimate_WithOnePizza_BakesAfterPreparation()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 0, (1, 0));

    Assert.Equal(new BatchEstimate(At(0), At(2), At(7)), Assert.Single(result));
  }

  [Fact]
  public void Estimate_WithSeveralPizzas_BakesWhenAllArePrepared()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 0, (3, 0));

    Assert.Equal(new BatchEstimate(At(0), At(6), At(11)), Assert.Single(result));
  }

  [Fact]
  public void Estimate_WithFutureEarliestStart_StartsAtEarliestStart()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 0, (1, 30));

    Assert.Equal(new BatchEstimate(At(30), At(32), At(37)), Assert.Single(result));
  }

  #endregion

  #region Pizze aperte

  [Fact]
  public void Estimate_PreparesNextBatchWhileCurrentOneBakes()
  {
    var baker = CreateBaker(preparationMinutes: 2, maxConcurrentPizzas: 3);
    var oven = CreateOven(capacity: 2, bakingMinutes: 5);

    var result = Estimate(baker, oven, now: 0, (2, 0), (2, 0));

    Assert.Equal(At(4), result[1].EstimatedStartAt);
  }

  [Fact]
  public void Estimate_WhenOpenPizzasAreFull_BakerWaitsForOvenExit()
  {
    var baker = CreateBaker(preparationMinutes: 2, maxConcurrentPizzas: 3);
    var oven = CreateOven(capacity: 2, bakingMinutes: 5);

    var result = Estimate(baker, oven, now: 0, (2, 0), (2, 0));

    Assert.Equal(new BatchEstimate(At(0), At(4), At(9)), result[0]);
    Assert.Equal(new BatchEstimate(At(4), At(11), At(16)), result[1]);
  }

  #endregion

  #region Regimi

  [Fact]
  public void Estimate_JuniorBaker_IsLimitedByOpenPizzas()
  {
    var junior = CreateBaker(preparationMinutes: 3, maxConcurrentPizzas: 4);
    var oven = CreateOven(capacity: 8, bakingMinutes: 4);

    var result = Estimate(junior, oven, now: 0, (4, 0), (4, 0), (4, 0));

    Assert.Equal([At(0), At(16), At(32)], result.Select(b => b.EstimatedStartAt));
    Assert.Equal([At(16), At(32), At(48)], result.Select(b => b.EstimatedReadyAt));
  }

  [Fact]
  public void Estimate_ExpertBaker_IsLimitedByPreparation()
  {
    var expert = CreateBaker(preparationMinutes: 1, maxConcurrentPizzas: 12);
    var oven = CreateOven(capacity: 8, bakingMinutes: 4);

    var result = Estimate(expert, oven, now: 0, (8, 0), (8, 0), (8, 0));

    Assert.Equal([At(0), At(8), At(16)], result.Select(b => b.EstimatedStartAt));
    Assert.Equal([At(12), At(20), At(28)], result.Select(b => b.EstimatedReadyAt));
  }

  [Fact]
  public void Estimate_SmallSlowOven_IsLimitedByOven()
  {
    var expert = CreateBaker(preparationMinutes: 1, maxConcurrentPizzas: 16);
    var smallOven = CreateOven(capacity: 6, bakingMinutes: 8);

    var result = Estimate(expert, smallOven, now: 0, (6, 0), (6, 0), (6, 0));

    Assert.Equal([At(6), At(14), At(22)], result.Select(b => b.EstimatedBakeStartAt));
    Assert.Equal([At(14), At(22), At(30)], result.Select(b => b.EstimatedReadyAt));
  }

  #endregion

  #region Forno

  [Fact]
  public void Estimate_WhenPostsAreEnough_BatchesBakeTogether()
  {
    var baker = CreateBaker(preparationMinutes: 1, maxConcurrentPizzas: 12);
    var oven = CreateOven(capacity: 8, bakingMinutes: 10);

    var result = Estimate(baker, oven, now: 0, (3, 0), (3, 0));

    Assert.Equal(new BatchEstimate(At(0), At(3), At(13)), result[0]);
    Assert.Equal(new BatchEstimate(At(3), At(6), At(16)), result[1]);
  }

  [Fact]
  public void Estimate_WhenOvenIsFull_PreparedBatchWaitsForPosts()
  {
    var baker = CreateBaker(preparationMinutes: 1, maxConcurrentPizzas: 16);
    var oven = CreateOven(capacity: 6, bakingMinutes: 8);

    var result = Estimate(baker, oven, now: 0, (6, 0), (6, 0));

    Assert.Equal(new BatchEstimate(At(6), At(14), At(22)), result[1]);
  }

  [Fact]
  public void Estimate_LaterBatchNeverEntersOvenBeforeEarlierOne()
  {
    var baker = CreateBaker(preparationMinutes: 1, maxConcurrentPizzas: 16);
    var oven = CreateOven(capacity: 6, bakingMinutes: 8);

    var result = Estimate(baker, oven, now: 0, (6, 0), (2, 0));

    Assert.Equal(At(6), result[0].EstimatedBakeStartAt);
    Assert.Equal(At(14), result[1].EstimatedBakeStartAt);
  }

  #endregion

  #region Infornate assegnate

  [Fact]
  public void Estimate_WithEarliestStartInThePast_StartsFromIt()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 0, (2, -2));

    Assert.Equal(new BatchEstimate(At(-2), At(2), At(7)), Assert.Single(result));
  }

  [Fact]
  public void Estimate_FutureBatchAfterAssignedOne_StartsWhenBakerIsFree()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 0, (2, -2), (2, 0));

    Assert.Equal(new BatchEstimate(At(2), At(6), At(11)), result[1]);
  }

  #endregion

  #region Infornata scaduta

  [Fact]
  public void Estimate_WhenBatchIsOverdue_ReadyAtIsNow()
  {
    var result = Estimate(CreateBaker(), CreateOven(), now: 20, (1, 0));

    Assert.Equal(new BatchEstimate(At(0), At(2), At(20)), Assert.Single(result));
  }

  [Fact]
  public void Estimate_WhenBatchIsOverdue_HoldsOvenForNextBatches()
  {
    var baker = CreateBaker(preparationMinutes: 2, maxConcurrentPizzas: 2);
    var oven = CreateOven(capacity: 1, bakingMinutes: 5);

    var result = Estimate(baker, oven, now: 8, (1, 0), (1, 1));

    Assert.Equal(new BatchEstimate(At(0), At(2), At(8)), result[0]);
    Assert.Equal(new BatchEstimate(At(2), At(8), At(13)), result[1]);
  }

  #endregion
}
