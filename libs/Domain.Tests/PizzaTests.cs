using Exceptions;

namespace Domain.Tests;

public class PizzaTests
{
  // Gli attributi C# non accettano decimal, MemberData permette di testare valori reali
  public static TheoryData<decimal> InvalidPrices => [0m, -0.01m, -6.50m];

  [Fact]
  public void Constructor_WithValidData_CreatesPizzaOnMenu()
  {
    var pizza = new Pizza("Margherita", "Tomato, mozzarella, basil", 6.50m);

    Assert.NotEqual(Guid.Empty, pizza.Id);
    Assert.Equal("Margherita", pizza.Name);
    Assert.Equal(6.50m, pizza.Price);
    Assert.True(pizza.IsOnMenu);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithMissingName_Throws(string? name)
  {
    Assert.Throws<DomainException>(() => new Pizza(name!, null, 6.50m));
  }

  [Theory]
  [MemberData(nameof(InvalidPrices))]
  public void Constructor_WithZeroOrNegativePrice_Throws(decimal price)
  {
    Assert.Throws<DomainException>(() => new Pizza("Margherita", null, price));
  }
}
