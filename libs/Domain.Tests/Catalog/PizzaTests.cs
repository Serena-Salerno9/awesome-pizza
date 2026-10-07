using Domain.Catalog;
using Domain.Exceptions;

namespace Domain.Tests.Catalog;

public class PizzaTests
{
  // Gli attributi C# non accettano decimal, MemberData permette di testare valori reali
  public static TheoryData<decimal> ZeroOrNegativePrices => [0m, -0.01m, -6.50m];
  public static TheoryData<decimal> PricesWithTooManyDecimals => [6.505m, 0.001m];

  #region Costruttore

  [Fact]
  public void Constructor_WithValidData_CreatesPizzaOnMenu()
  {
    var pizza = new Pizza("Margherita", "Tomato, mozzarella, basil", 6.50m);

    Assert.NotEqual(Guid.Empty, pizza.Id);
    Assert.Equal("Margherita", pizza.Name);
    Assert.Equal("Tomato, mozzarella, basil", pizza.Description);
    Assert.Equal(6.50m, pizza.Price);
    Assert.True(pizza.IsOnMenu);
  }

  #endregion

  #region Nome

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithMissingName_Throws(string? name)
  {
    Assert.Throws<DomainException>(() => new Pizza(name!, null, 6.50m));
  }

  [Fact]
  public void Constructor_TrimsName()
  {
    var pizza = new Pizza("  Margherita  ", null, 6.50m);

    Assert.Equal("Margherita", pizza.Name);
  }

  [Fact]
  public void Constructor_WithNameAtMaxLengthAfterTrim_Accepts()
  {
    var name = "  " + new string('a', Pizza.NameMaxLength) + "  ";

    var pizza = new Pizza(name, null, 6.50m);

    Assert.Equal(Pizza.NameMaxLength, pizza.Name.Length);
  }

  [Fact]
  public void Constructor_WithNameTooLong_Throws()
  {
    var name = new string('a', Pizza.NameMaxLength + 1);

    Assert.Throws<DomainException>(() => new Pizza(name, null, 6.50m));
  }

  #endregion

  #region Descrizione

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithBlankDescription_SetsDescriptionToNull(string? description)
  {
    var pizza = new Pizza("Margherita", description, 6.50m);

    Assert.Null(pizza.Description);
  }

  [Fact]
  public void Constructor_WithDescriptionAtMaxLengthAfterTrim_Accepts()
  {
    var description = "  " + new string('a', Pizza.DescriptionMaxLength) + "  ";

    var pizza = new Pizza("Margherita", description, 6.50m);

    Assert.Equal(Pizza.DescriptionMaxLength, pizza.Description!.Length);
  }

  [Fact]
  public void Constructor_WithDescriptionTooLong_Throws()
  {
    var description = new string('a', Pizza.DescriptionMaxLength + 1);

    Assert.Throws<DomainException>(() => new Pizza("Margherita", description, 6.50m));
  }

  #endregion

  #region Prezzo

  [Theory]
  [MemberData(nameof(ZeroOrNegativePrices))]
  public void Constructor_WithZeroOrNegativePrice_Throws(decimal price)
  {
    Assert.Throws<DomainException>(() => new Pizza("Margherita", null, price));
  }

  [Theory]
  [MemberData(nameof(PricesWithTooManyDecimals))]
  public void Constructor_WithTooManyDecimals_Throws(decimal price)
  {
    Assert.Throws<DomainException>(() => new Pizza("Margherita", null, price));
  }

  [Fact]
  public void Constructor_WithTrailingZeroDecimals_Accepts()
  {
    var pizza = new Pizza("Margherita", null, 6.500m);

    Assert.Equal(6.50m, pizza.Price);
  }

  #endregion

  #region Menu

  [Fact]
  public void RemoveFromMenu_WhenOnMenu_SetsIsOnMenuToFalse()
  {
    var pizza = new Pizza("Margherita", null, 6.50m);

    pizza.RemoveFromMenu();

    Assert.False(pizza.IsOnMenu);
  }

  [Fact]
  public void RemoveFromMenu_WhenAlreadyOffMenu_RemainsOffMenu()
  {
    var pizza = new Pizza("Margherita", null, 6.50m);
    pizza.RemoveFromMenu();

    pizza.RemoveFromMenu();

    Assert.False(pizza.IsOnMenu);
  }

  [Fact]
  public void AddBackToMenu_WhenOffMenu_SetsIsOnMenuToTrue()
  {
    var pizza = new Pizza("Margherita", null, 6.50m);
    pizza.RemoveFromMenu();

    pizza.AddBackToMenu();

    Assert.True(pizza.IsOnMenu);
  }

  [Fact]
  public void AddBackToMenu_WhenAlreadyOnMenu_RemainsOnMenu()
  {
    var pizza = new Pizza("Margherita", null, 6.50m);

    pizza.AddBackToMenu();

    Assert.True(pizza.IsOnMenu);
  }

  #endregion
}
