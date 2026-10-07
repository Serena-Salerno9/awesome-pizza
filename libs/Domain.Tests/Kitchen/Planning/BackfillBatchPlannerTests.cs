using Domain.Kitchen;
using Domain.Kitchen.Planning;

namespace Domain.Tests.Kitchen.Planning;

public class BackfillBatchPlannerTests
{
  private static readonly DateTimeOffset Noon = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

  private static DateTimeOffset At(int minutes) => Noon.AddMinutes(minutes);

  // Forno piccolo: il massimo per infornata è 6 pizze, 1' di preparazione, 8' di cottura
  private static readonly Baker TestBaker = new("Mario", 16, TimeSpan.FromMinutes(1));
  private static readonly Oven SmallOven = new(6, TimeSpan.FromMinutes(8));

  private static PlanningContext CreateContext(params BatchToEstimate[] assigned) =>
    new(6, new KitchenState(TestBaker, SmallOven, At(0), assigned));

  private static PendingOrder CreateOrder(params int[] lineQuantities) =>
    new(Guid.NewGuid(), lineQuantities.Select(q => new PendingLine(Guid.NewGuid(), q)).ToList());

  private static IReadOnlyList<PlannedBatch> Plan(int toleranceMinutes, PlanningContext context, params PendingOrder[] queue) =>
    new BackfillBatchPlanner(TimeSpan.FromMinutes(toleranceMinutes)).Plan(queue, context);

  private static IEnumerable<int> Sizes(IReadOnlyList<PlannedBatch> batches) =>
    batches.Select(b => b.PizzaCount);

  #region Validazione

  [Fact]
  public void Constructor_WithNegativeTolerance_Throws()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new BackfillBatchPlanner(TimeSpan.FromMinutes(-1)));
  }

  [Fact]
  public void Plan_WithNullQueue_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new BackfillBatchPlanner(TimeSpan.Zero).Plan(null!, CreateContext()));
  }

  [Fact]
  public void Plan_WithNullContext_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new BackfillBatchPlanner(TimeSpan.Zero).Plan([CreateOrder(3)], null!));
  }

  [Fact]
  public void Plan_WithoutKitchenState_Throws()
  {
    Assert.Throws<ArgumentException>(() => Plan(2, new PlanningContext(6), CreateOrder(3)));
  }

  [Fact]
  public void Plan_WithEmptyQueue_ReturnsEmpty()
  {
    var result = Plan(2, CreateContext());

    Assert.Empty(result);
  }

  #endregion

  #region Tolleranza

  // FIFO: [4], [4+2] con uscite alle 12, 20, 20. Anticipare l'ordine da 2 nella prima infornata
  // la porta a 6 pizze: esce alle 14 invece che alle 12 (+2), e la seconda alle 22 invece che alle 20 (+2)
  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  public void Plan_WhenSomeoneWouldBeWorseThanTolerance_KeepsFifoPlan(int toleranceMinutes)
  {
    var result = Plan(toleranceMinutes, CreateContext(), CreateOrder(4), CreateOrder(4), CreateOrder(2));

    Assert.Equal([4, 6], Sizes(result));
  }

  [Fact]
  public void Plan_WhenWorseningEqualsTolerance_PullsSmallOrderForward()
  {
    var first = CreateOrder(4);
    var small = CreateOrder(2);

    var result = Plan(2, CreateContext(), first, CreateOrder(4), small);

    Assert.Equal([6, 4], Sizes(result));
    Assert.Equal(
      [first.Lines[0].OrderLineId, small.Lines[0].OrderLineId],
      result[0].Lines.Select(l => l.OrderLineId));
  }

  #endregion

  #region Forno occupato

  // Le infornate vanno in sequenza: anche con un'infornata già assegnata, aggiungere pizze alla prima
  // pianificata ne allunga la preparazione e fa slittare gli altri, quindi con T = 0 non si anticipa nulla
  [Fact]
  public void Plan_WhenAnAssignedBatchIsOpen_DoesNotPullSmallOrderWithZeroTolerance()
  {
    var small = CreateOrder(2);
    var assigned = new BatchToEstimate(6, At(0));

    var result = Plan(0, CreateContext(assigned), CreateOrder(3), CreateOrder(4), small);

    Assert.Equal([3, 6], Sizes(result));
    Assert.DoesNotContain(small.Lines[0].OrderLineId, result[0].Lines.Select(l => l.OrderLineId));
  }

  #endregion

  #region Limiti

  [Fact]
  public void Plan_DoesNotSplitOrdersToFillFreeSpots()
  {
    var result = Plan(60, CreateContext(), CreateOrder(4), CreateOrder(3), CreateOrder(3));

    Assert.Equal([4, 6], Sizes(result));
  }

  [Fact]
  public void Plan_WhenNoLaterOrderFits_KeepsFifoPlan()
  {
    var result = Plan(60, CreateContext(), CreateOrder(5), CreateOrder(5));

    Assert.Equal([5, 5], Sizes(result));
  }

  [Fact]
  public void Plan_WithSingleBatch_ReturnsIt()
  {
    var result = Plan(2, CreateContext(), CreateOrder(3));

    Assert.Equal([3], Sizes(result));
  }

  #endregion

  #region Invarianti

  [Fact]
  public void Plan_PreservesPizzaCountPerLineAndRespectsMaxSize()
  {
    var queue = new[] { CreateOrder(4), CreateOrder(3), CreateOrder(2), CreateOrder(10, 4), CreateOrder(1), CreateOrder(5) };

    var result = Plan(60, CreateContext(), queue);

    var expected = queue.SelectMany(o => o.Lines).ToDictionary(l => l.OrderLineId, l => l.Quantity);
    var actual = result
      .SelectMany(b => b.Lines)
      .GroupBy(l => l.OrderLineId)
      .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
    Assert.Equal(expected, actual);
    Assert.All(result, b => Assert.InRange(b.PizzaCount, 1, 6));
  }

  #endregion
}
