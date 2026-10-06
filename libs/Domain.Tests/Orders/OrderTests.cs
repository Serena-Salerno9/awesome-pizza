using Domain.Catalog;
using Domain.Exceptions;
using Domain.Orders;

namespace Domain.Tests.Orders;

public class OrderTests
{
  private static readonly DateOnly Today = new(2026, 10, 4);
  private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
  private static readonly DateTimeOffset Later = Now.AddMinutes(10);
  private static readonly Guid WorkstationId = Guid.NewGuid();

  private static Pizza CreatePizza(string name = "Margherita", decimal price = 6.50m) =>
    new(name, null, price);

  private static Order CreateOrder(int dailyNumber = 1) =>
    new(Today, dailyNumber, [(CreatePizza(), 1)], Now);

  #region Costruttore

  [Fact]
  public void Constructor_WithValidData_CreatesQueuedOrder()
  {
    var order = new Order(Today, 7, [(CreatePizza(), 2)], Now);

    Assert.NotEqual(Guid.Empty, order.Id);
    Assert.NotEqual(Guid.Empty, order.Version);
    Assert.Equal(Today, order.BusinessDate);
    Assert.Equal(7, order.DailyNumber);
    Assert.Equal(OrderStatus.Queued, order.Status);
    Assert.Equal(Now, order.CreatedAt);
    Assert.Null(order.StartedAt);
    Assert.Null(order.ReadyAt);
    Assert.Null(order.FkWorkstation);
    Assert.Single(order.Lines);
  }

  [Fact]
  public void Constructor_WithNullItems_Throws()
  {
    Assert.Throws<ArgumentNullException>(() => new Order(Today, 1, null!, Now));
  }

  #endregion

  #region Numero giornaliero e codice

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Constructor_WithDailyNumberBelowOne_Throws(int dailyNumber)
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => CreateOrder(dailyNumber));
  }

  [Fact]
  public void Constructor_WithDailyNumberAtMax_Accepts()
  {
    var order = CreateOrder(Order.MaxDailyOrders);

    Assert.Equal(Order.MaxDailyOrders, order.DailyNumber);
  }

  [Fact]
  public void Constructor_WithDailyNumberAboveMax_Throws()
  {
    Assert.Throws<DomainException>(() => CreateOrder(Order.MaxDailyOrders + 1));
  }

  [Theory]
  [InlineData(1, "001")]
  [InlineData(42, "042")]
  [InlineData(999, "999")]
  public void Code_IsDailyNumberPaddedToThreeDigits(int dailyNumber, string expected)
  {
    var order = CreateOrder(dailyNumber);

    Assert.Equal(expected, order.Code);
  }

  #endregion

  #region Righe

  [Fact]
  public void Constructor_WithNoItems_Throws()
  {
    Assert.Throws<DomainException>(() => new Order(Today, 1, [], Now));
  }

  [Fact]
  public void Constructor_WithNullPizza_Throws()
  {
    Assert.Throws<DomainException>(() => new Order(Today, 1, [(null!, 1)], Now));
  }

  [Fact]
  public void Constructor_WithPizzaOffMenu_Throws()
  {
    var pizza = CreatePizza();
    pizza.RemoveFromMenu();

    Assert.Throws<DomainException>(() => new Order(Today, 1, [(pizza, 1)], Now));
  }

  [Fact]
  public void Constructor_WithMultiplePizzas_CreatesOneLinePerItem()
  {
    var order = new Order(
      Today,
      1,
      [(CreatePizza("Margherita"), 1), (CreatePizza("Diavola", 8m), 2)],
      Now);

    Assert.Equal(2, order.Lines.Count);
  }

  #endregion

  #region Limite pizze

  [Fact]
  public void Constructor_WithPizzaCountAtMax_Accepts()
  {
    var order = new Order(Today, 1, [(CreatePizza(), Order.MaxPizzasPerOrder)], Now);

    Assert.Equal(Order.MaxPizzasPerOrder, order.PizzaCount);
  }

  [Fact]
  public void Constructor_WithPizzaCountAboveMax_Throws()
  {
    Assert.Throws<DomainException>(
      () => new Order(Today, 1, [(CreatePizza(), Order.MaxPizzasPerOrder + 1)], Now));
  }

  [Fact]
  public void Constructor_WithPizzaCountAboveMaxAcrossLines_Throws()
  {
    var items = new[]
    {
      (CreatePizza("Margherita"), Order.MaxPizzasPerOrder),
      (CreatePizza("Diavola", 8m), 1)
    };

    Assert.Throws<DomainException>(() => new Order(Today, 1, items, Now));
  }

  #endregion

  #region Totali

  [Fact]
  public void PizzaCount_SumsQuantitiesOfAllLines()
  {
    var order = new Order(
      Today,
      1,
      [(CreatePizza("Margherita"), 2), (CreatePizza("Diavola", 8m), 3)],
      Now);

    Assert.Equal(5, order.PizzaCount);
  }

  [Fact]
  public void Total_SumsLineTotals()
  {
    var order = new Order(
      Today,
      1,
      [(CreatePizza("Margherita", 6.50m), 2), (CreatePizza("Diavola", 8m), 3)],
      Now);

    Assert.Equal(37m, order.Total);
  }

  #endregion

  #region Preparazione

  [Fact]
  public void StartPreparation_WhenQueued_MovesToInPreparation()
  {
    var order = CreateOrder();

    order.StartPreparation(WorkstationId, Later);

    Assert.Equal(OrderStatus.InPreparation, order.Status);
    Assert.Equal(Later, order.StartedAt);
    Assert.Null(order.ReadyAt);
  }

  [Fact]
  public void StartPreparation_AssignsWorkstation()
  {
    var order = CreateOrder();

    order.StartPreparation(WorkstationId, Later);

    Assert.Equal(WorkstationId, order.FkWorkstation);
  }

  [Fact]
  public void StartPreparation_WithEmptyWorkstation_ThrowsAndLeavesOrderUnchanged()
  {
    var order = CreateOrder();
    var version = order.Version;

    Assert.Throws<DomainException>(() => order.StartPreparation(Guid.Empty, Later));

    Assert.Equal(OrderStatus.Queued, order.Status);
    Assert.Null(order.FkWorkstation);
    Assert.Null(order.StartedAt);
    Assert.Equal(version, order.Version);
  }

  [Fact]
  public void StartPreparation_ChangesVersion()
  {
    var order = CreateOrder();
    var version = order.Version;

    order.StartPreparation(WorkstationId, Later);

    Assert.NotEqual(version, order.Version);
  }

  [Fact]
  public void StartPreparation_WhenAlreadyInPreparation_Throws()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Later);

    Assert.Throws<DomainException>(() => order.StartPreparation(WorkstationId, Later));
  }

  [Fact]
  public void StartPreparation_WhenReady_Throws()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Later);
    order.MarkReady(Later);

    Assert.Throws<DomainException>(() => order.StartPreparation(WorkstationId, Later));
  }

  [Fact]
  public void StartPreparation_BeforeCreatedAt_ThrowsAndLeavesOrderUnchanged()
  {
    var order = CreateOrder();

    Assert.Throws<DomainException>(() => order.StartPreparation(WorkstationId, Now.AddMinutes(-1)));

    Assert.Equal(OrderStatus.Queued, order.Status);
    Assert.Null(order.StartedAt);
  }

  [Fact]
  public void StartPreparation_AtCreatedAt_Accepts()
  {
    var order = CreateOrder();

    order.StartPreparation(WorkstationId, Now);

    Assert.Equal(Now, order.StartedAt);
  }

  #endregion

  #region Presa in carico

  [Fact]
  public void TakeCharge_WhenInPreparation_SetsTakenAt()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);

    order.TakeCharge(Later);

    Assert.Equal(OrderStatus.InPreparation, order.Status);
    Assert.Equal(Later, order.TakenAt);
  }

  [Fact]
  public void TakeCharge_ChangesVersion()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    var version = order.Version;

    order.TakeCharge(Later);

    Assert.NotEqual(version, order.Version);
  }

  [Fact]
  public void TakeCharge_AtStartedAt_Accepts()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);

    order.TakeCharge(Now);

    Assert.Equal(Now, order.TakenAt);
  }

  [Fact]
  public void TakeCharge_BeforeStartedAt_ThrowsAndLeavesOrderUnchanged()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Later);
    var version = order.Version;

    Assert.Throws<DomainException>(() => order.TakeCharge(Now));

    Assert.Null(order.TakenAt);
    Assert.Equal(version, order.Version);
  }

  [Fact]
  public void TakeCharge_WhenQueued_ThrowsAndLeavesOrderUnchanged()
  {
    var order = CreateOrder();
    var version = order.Version;

    Assert.Throws<DomainException>(() => order.TakeCharge(Later));

    Assert.Null(order.TakenAt);
    Assert.Equal(version, order.Version);
  }

  [Fact]
  public void TakeCharge_WhenAlreadyTaken_Throws()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    order.TakeCharge(Later);

    Assert.Throws<DomainException>(() => order.TakeCharge(Later.AddMinutes(1)));
  }

  [Fact]
  public void TakeCharge_WhenReady_Throws()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    order.MarkReady(Later);

    Assert.Throws<DomainException>(() => order.TakeCharge(Later));
  }

  #endregion

  #region Pronto

  [Fact]
  public void MarkReady_WhenInPreparation_MovesToReady()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);

    order.MarkReady(Later);

    Assert.Equal(OrderStatus.Ready, order.Status);
    Assert.Equal(Now, order.StartedAt);
    Assert.Equal(Later, order.ReadyAt);
  }

  [Fact]
  public void MarkReady_ChangesVersion()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    var version = order.Version;

    order.MarkReady(Later);

    Assert.NotEqual(version, order.Version);
  }

  [Fact]
  public void MarkReady_WhenQueued_ThrowsAndLeavesOrderUnchanged()
  {
    var order = CreateOrder();
    var version = order.Version;

    Assert.Throws<DomainException>(() => order.MarkReady(Later));

    Assert.Equal(OrderStatus.Queued, order.Status);
    Assert.Null(order.ReadyAt);
    Assert.Equal(version, order.Version);
  }

  [Fact]
  public void MarkReady_WhenAlreadyReady_Throws()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    order.MarkReady(Later);

    Assert.Throws<DomainException>(() => order.MarkReady(Later.AddMinutes(1)));
  }

  [Fact]
  public void MarkReady_BeforeStartedAt_ThrowsAndLeavesOrderUnchanged()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Later);
    var version = order.Version;

    Assert.Throws<DomainException>(() => order.MarkReady(Now));

    Assert.Equal(OrderStatus.InPreparation, order.Status);
    Assert.Null(order.ReadyAt);
    Assert.Equal(version, order.Version);
  }

  [Fact]
  public void MarkReady_BeforeTakenAt_Throws()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    order.TakeCharge(Later);

    Assert.Throws<DomainException>(() => order.MarkReady(Now.AddMinutes(5)));
  }

  [Fact]
  public void MarkReady_AtTakenAt_Accepts()
  {
    var order = CreateOrder();
    order.StartPreparation(WorkstationId, Now);
    order.TakeCharge(Later);

    order.MarkReady(Later);

    Assert.Equal(Later, order.ReadyAt);
  }

  #endregion
}
