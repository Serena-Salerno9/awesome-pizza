using Domain.Kitchen.Planning;

namespace Domain.Tests.Kitchen.Planning;

public class FifoBatchPlannerTests
{
  private static readonly PlanningContext Context = new(MaxBatchSize: 8);

  private static PendingOrder CreateOrder(params int[] lineQuantities) =>
    new(Guid.NewGuid(), lineQuantities.Select(q => new PendingLine(Guid.NewGuid(), q)).ToList());

  private static IReadOnlyList<PlannedBatch> Plan(params PendingOrder[] queue) =>
    new FifoBatchPlanner().Plan(queue, Context);

  private static IEnumerable<int> Sizes(IReadOnlyList<PlannedBatch> batches) =>
    batches.Select(b => b.PizzaCount);

  #region Validazione

  [Fact]
  public void Plan_WithEmptyQueue_ReturnsEmpty()
  {
    var result = Plan();

    Assert.Empty(result);
  }

  [Fact]
  public void Plan_WithNullQueue_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new FifoBatchPlanner().Plan(null!, Context));
  }

  [Fact]
  public void Plan_WithNullContext_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new FifoBatchPlanner().Plan([CreateOrder(3)], null!));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Plan_WithMaxBatchSizeBelowOne_Throws(int maxBatchSize)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() =>
      new FifoBatchPlanner().Plan([CreateOrder(3)], new PlanningContext(maxBatchSize)));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Plan_WithLineQuantityBelowOne_Throws(int quantity)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => Plan(CreateOrder(quantity)));
  }

  #endregion

  #region Ordini che ci stanno

  [Fact]
  public void Plan_WithOneSmallOrder_ReturnsOneBatch()
  {
    var result = Plan(CreateOrder(3));

    Assert.Equal([3], Sizes(result));
  }

  [Fact]
  public void Plan_WithOrdersThatFitTogether_SharesTheBatch()
  {
    var result = Plan(CreateOrder(3), CreateOrder(4));

    Assert.Equal([7], Sizes(result));
  }

  [Fact]
  public void Plan_WhenNextOrderDoesNotFit_ClosesBatchWithoutSplittingIt()
  {
    var result = Plan(CreateOrder(5), CreateOrder(4));

    Assert.Equal([5, 4], Sizes(result));
  }

  [Fact]
  public void Plan_WhenBatchIsExactlyFull_NextOrderOpensNewBatch()
  {
    var result = Plan(CreateOrder(8), CreateOrder(2));

    Assert.Equal([8, 2], Sizes(result));
  }

  [Fact]
  public void Plan_KeepsLinesOfTheSameOrderTogether()
  {
    var order = CreateOrder(2, 3);

    var batch = Assert.Single(Plan(order));

    Assert.Equal(order.Lines.Select(l => new PlannedBatchLine(l.OrderLineId, l.Quantity)), batch.Lines);
  }

  #endregion

  #region Ordini grandi

  [Fact]
  public void Plan_WithOrderAboveMaxSize_SplitsItInConsecutiveBatches()
  {
    var result = Plan(CreateOrder(10));

    Assert.Equal([8, 2], Sizes(result));
  }

  [Fact]
  public void Plan_WithOrderMultipleOfMaxSize_DoesNotLeaveEmptyBatch()
  {
    var result = Plan(CreateOrder(16));

    Assert.Equal([8, 8], Sizes(result));
  }

  [Fact]
  public void Plan_WithBigOrderAfterSmallOne_FillsFreeSpots()
  {
    var small = CreateOrder(3);
    var big = CreateOrder(10);

    var result = Plan(small, big);

    var smallLine = small.Lines[0].OrderLineId;
    var bigLine = big.Lines[0].OrderLineId;
    Assert.Equal([new PlannedBatchLine(smallLine, 3), new PlannedBatchLine(bigLine, 5)], result[0].Lines);
    Assert.Equal([new PlannedBatchLine(bigLine, 5)], result[1].Lines);
  }

  [Fact]
  public void Plan_WithSmallOrderAfterBigOne_SharesTheRemainder()
  {
    var big = CreateOrder(10);
    var small = CreateOrder(3);

    var result = Plan(big, small);

    var bigLine = big.Lines[0].OrderLineId;
    var smallLine = small.Lines[0].OrderLineId;
    Assert.Equal([new PlannedBatchLine(bigLine, 8)], result[0].Lines);
    Assert.Equal([new PlannedBatchLine(bigLine, 2), new PlannedBatchLine(smallLine, 3)], result[1].Lines);
  }

  [Fact]
  public void Plan_WhenBatchIsFull_BigOrderStartsNewBatch()
  {
    var result = Plan(CreateOrder(8), CreateOrder(10));

    Assert.Equal([8, 8, 2], Sizes(result));
  }

  [Fact]
  public void Plan_WithBigOrderOfSeveralLines_SplitsInTheMiddleOfALine()
  {
    var order = CreateOrder(6, 4);

    var result = Plan(order);

    var first = order.Lines[0].OrderLineId;
    var second = order.Lines[1].OrderLineId;
    Assert.Equal([new PlannedBatchLine(first, 6), new PlannedBatchLine(second, 2)], result[0].Lines);
    Assert.Equal([new PlannedBatchLine(second, 2)], result[1].Lines);
  }

  #endregion

  #region FIFO non guarda avanti

  [Fact]
  public void Plan_DoesNotMoveSmallerOrdersIntoEarlierBatches()
  {
    var result = Plan(CreateOrder(6), CreateOrder(4), CreateOrder(2));

    Assert.Equal([6, 6], Sizes(result));
  }

  #endregion

  #region Invarianti

  [Fact]
  public void Plan_PreservesPizzaCountPerLineAndRespectsMaxSize()
  {
    var queue = new[] { CreateOrder(3), CreateOrder(10, 4), CreateOrder(5), CreateOrder(2, 2, 2) };

    var result = Plan(queue);

    var expected = queue.SelectMany(o => o.Lines).ToDictionary(l => l.OrderLineId, l => l.Quantity);
    var actual = result
      .SelectMany(b => b.Lines)
      .GroupBy(l => l.OrderLineId)
      .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
    Assert.Equal(expected, actual);
    Assert.All(result, b => Assert.InRange(b.PizzaCount, 1, Context.MaxBatchSize));
  }

  #endregion
}
