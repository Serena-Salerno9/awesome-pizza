using Domain.Catalog;
using Domain.Exceptions;
using Domain.Kitchen;
using Domain.Orders;

namespace Domain.Tests.Kitchen;

public class BatchTests
{
  private static readonly DateOnly Today = new(2026, 10, 6);
  private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
  private static readonly DateTimeOffset Later = Now.AddMinutes(10);
  private static readonly Guid WorkstationId = Guid.NewGuid();

  private static OrderLine CreateOrderLine(int quantity = 2, int dailyNumber = 1) =>
    new Order(Today, dailyNumber, [(new Pizza("Margherita", null, 6.50m), quantity)], Now).Lines.Single();

  private static Batch CreateBatch(DateTimeOffset? startedAt = null)
  {
    var line = CreateOrderLine();
    return new Batch(WorkstationId, [(line, line.Quantity)], startedAt ?? Now);
  }

  #region Costruttore

  [Fact]
  public void Constructor_WithValidData_CreatesBatch()
  {
    var line = CreateOrderLine(2);

    var batch = new Batch(WorkstationId, [(line, 2)], Now);

    Assert.NotEqual(Guid.Empty, batch.Id);
    Assert.NotEqual(Guid.Empty, batch.Version);
    Assert.Equal(WorkstationId, batch.FkWorkstation);
    Assert.Equal(Now, batch.StartedAt);
    Assert.Null(batch.TakenAt);
    Assert.Null(batch.ReadyAt);
    var batchLine = Assert.Single(batch.Lines);
    Assert.Equal(batch.Id, batchLine.FkBatch);
    Assert.Equal(line.Id, batchLine.FkOrderLine);
    Assert.Equal(2, batchLine.Quantity);
  }

  [Fact]
  public void Constructor_WithEmptyWorkstation_Throws()
  {
    var line = CreateOrderLine();

    Assert.Throws<DomainException>(() => new Batch(Guid.Empty, [(line, 1)], Now));
  }

  [Fact]
  public void Constructor_WithNullItems_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new Batch(WorkstationId, null!, Now));
  }

  #endregion

  #region Righe

  [Fact]
  public void Constructor_WithNoItems_Throws()
  {
    Assert.Throws<DomainException>(() => new Batch(WorkstationId, [], Now));
  }

  [Fact]
  public void Constructor_WithNullLine_Throws()
  {
    Assert.Throws<DomainException>(() => new Batch(WorkstationId, [(null!, 1)], Now));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Constructor_WithQuantityBelowOne_Throws(int quantity)
  {
    var line = CreateOrderLine();

    Assert.Throws<DomainException>(() => new Batch(WorkstationId, [(line, quantity)], Now));
  }

  [Fact]
  public void Constructor_WithQuantityAboveOrderLine_Throws()
  {
    var line = CreateOrderLine(2);

    Assert.Throws<DomainException>(() => new Batch(WorkstationId, [(line, 3)], Now));
  }

  [Fact]
  public void Constructor_WithPartOfOrderLine_Accepts()
  {
    var line = CreateOrderLine(8);

    var batch = new Batch(WorkstationId, [(line, 6)], Now);

    Assert.Equal(6, batch.PizzaCount);
  }

  [Fact]
  public void Constructor_WithSameOrderLineTwice_Throws()
  {
    var line = CreateOrderLine(4);

    Assert.Throws<DomainException>(() => new Batch(WorkstationId, [(line, 1), (line, 1)], Now));
  }

  [Fact]
  public void Constructor_WithLinesFromDifferentOrders_SumsPizzaCount()
  {
    var first = CreateOrderLine(2, dailyNumber: 1);
    var second = CreateOrderLine(1, dailyNumber: 2);

    var batch = new Batch(WorkstationId, [(first, 2), (second, 1)], Now);

    Assert.Equal(2, batch.Lines.Count);
    Assert.Equal(3, batch.PizzaCount);
  }

  #endregion

  #region Presa in carico

  [Fact]
  public void TakeCharge_SetsTakenAt()
  {
    var batch = CreateBatch();

    batch.TakeCharge(Later);

    Assert.Equal(Later, batch.TakenAt);
  }

  [Fact]
  public void TakeCharge_ChangesVersion()
  {
    var batch = CreateBatch();
    var version = batch.Version;

    batch.TakeCharge(Later);

    Assert.NotEqual(version, batch.Version);
  }

  [Fact]
  public void TakeCharge_AtStartedAt_Accepts()
  {
    var batch = CreateBatch();

    batch.TakeCharge(Now);

    Assert.Equal(Now, batch.TakenAt);
  }

  [Fact]
  public void TakeCharge_BeforeStartedAt_ThrowsAndLeavesBatchUnchanged()
  {
    var batch = CreateBatch(Later);
    var version = batch.Version;

    Assert.Throws<DomainException>(() => batch.TakeCharge(Now));

    Assert.Null(batch.TakenAt);
    Assert.Equal(version, batch.Version);
  }

  [Fact]
  public void TakeCharge_WhenAlreadyTaken_Throws()
  {
    var batch = CreateBatch();
    batch.TakeCharge(Later);

    Assert.Throws<DomainException>(() => batch.TakeCharge(Later.AddMinutes(1)));
  }

  [Fact]
  public void TakeCharge_WhenReady_Throws()
  {
    var batch = CreateBatch();
    batch.MarkReady(Later);

    Assert.Throws<DomainException>(() => batch.TakeCharge(Later));
  }

  #endregion

  #region Pronto

  [Fact]
  public void MarkReady_SetsReadyAt()
  {
    var batch = CreateBatch();

    batch.MarkReady(Later);

    Assert.Equal(Later, batch.ReadyAt);
  }

  [Fact]
  public void MarkReady_ChangesVersion()
  {
    var batch = CreateBatch();
    var version = batch.Version;

    batch.MarkReady(Later);

    Assert.NotEqual(version, batch.Version);
  }

  [Fact]
  public void MarkReady_BeforeStartedAt_ThrowsAndLeavesBatchUnchanged()
  {
    var batch = CreateBatch(Later);
    var version = batch.Version;

    Assert.Throws<DomainException>(() => batch.MarkReady(Now));

    Assert.Null(batch.ReadyAt);
    Assert.Equal(version, batch.Version);
  }

  [Fact]
  public void MarkReady_BeforeTakenAt_ThrowsEvenIfAfterStartedAt()
  {
    var batch = CreateBatch();
    batch.TakeCharge(Later);

    Assert.Throws<DomainException>(() => batch.MarkReady(Now.AddMinutes(5)));
  }

  [Fact]
  public void MarkReady_AtTakenAt_Accepts()
  {
    var batch = CreateBatch();
    batch.TakeCharge(Later);

    batch.MarkReady(Later);

    Assert.Equal(Later, batch.ReadyAt);
  }

  [Fact]
  public void MarkReady_WhenAlreadyReady_Throws()
  {
    var batch = CreateBatch();
    batch.MarkReady(Later);

    Assert.Throws<DomainException>(() => batch.MarkReady(Later.AddMinutes(1)));
  }

  #endregion
}
