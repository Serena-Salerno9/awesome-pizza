using Domain.Catalog;
using Domain.Exceptions;
using Domain.Orders;

namespace Domain.Tests.Orders;

public class OrderLineTests
{
  private static readonly DateOnly Today = new(2026, 10, 4);
  private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

  private static Order CreateOrder(Pizza pizza, int quantity) =>
    new(Today, 1, [(pizza, quantity)], Now);

  #region Costruttore

  [Fact]
  public void Line_CreatedFromOrder_CopiesPizzaSnapshot()
  {
    var pizza = new Pizza("Margherita", null, 6.50m);

    var order = CreateOrder(pizza, 2);

    var line = Assert.Single(order.Lines);
    Assert.NotEqual(Guid.Empty, line.Id);
    Assert.Equal(order.Id, line.FkOrder);
    Assert.Equal("Margherita", line.PizzaName);
    Assert.Equal(6.50m, line.UnitPrice);
    Assert.Equal(2, line.Quantity);
  }

  #endregion

  #region Quantità

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Line_WithQuantityBelowOne_Throws(int quantity)
  {
    var pizza = new Pizza("Margherita", null, 6.50m);

    Assert.Throws<DomainException>(() => CreateOrder(pizza, quantity));
  }

  #endregion

  #region Totale riga

  [Fact]
  public void LineTotal_ReturnsUnitPriceTimesQuantity()
  {
    var pizza = new Pizza("Margherita", null, 6.50m);

    var order = CreateOrder(pizza, 3);

    Assert.Equal(19.50m, order.Lines.Single().LineTotal);
  }

  #endregion
}
