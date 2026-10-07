using Domain.Exceptions;
using Domain.Kitchen;

namespace Domain.Tests.Kitchen;

public class BakerTests
{
  private static readonly TimeSpan PreparationTime = TimeSpan.FromMinutes(3);

  // Gli attributi C# non accettano TimeSpan, MemberData permette di testare valori reali
  public static TheoryData<TimeSpan> ZeroOrNegativeDurations => [TimeSpan.Zero, TimeSpan.FromSeconds(-1), TimeSpan.FromMinutes(-3)];

  #region Costruttore

  [Fact]
  public void Constructor_WithValidData_CreatesBaker()
  {
    var baker = new Baker("Mario", 3, PreparationTime);

    Assert.NotEqual(Guid.Empty, baker.Id);
    Assert.Equal("Mario", baker.Name);
    Assert.Equal(3, baker.MaxConcurrentPizzas);
    Assert.Equal(PreparationTime, baker.PreparationTimePerPizza);
  }

  #endregion

  #region Nome

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithMissingName_Throws(string? name)
  {
    Assert.Throws<DomainException>(() => new Baker(name!, 3, PreparationTime));
  }

  [Fact]
  public void Constructor_TrimsName()
  {
    var baker = new Baker("  Mario  ", 3, PreparationTime);

    Assert.Equal("Mario", baker.Name);
  }

  [Fact]
  public void Constructor_WithNameAtMaxLengthAfterTrim_Accepts()
  {
    var name = "  " + new string('a', Baker.NameMaxLength) + "  ";

    var baker = new Baker(name, 3, PreparationTime);

    Assert.Equal(Baker.NameMaxLength, baker.Name.Length);
  }

  [Fact]
  public void Constructor_WithNameTooLong_Throws()
  {
    var name = new string('a', Baker.NameMaxLength + 1);

    Assert.Throws<DomainException>(() => new Baker(name, 3, PreparationTime));
  }

  #endregion

  #region Pizze gestibili

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Constructor_WithMaxConcurrentPizzasBelowOne_Throws(int maxConcurrentPizzas)
  {
    Assert.Throws<DomainException>(() => new Baker("Mario", maxConcurrentPizzas, PreparationTime));
  }

  [Fact]
  public void Constructor_WithMaxConcurrentPizzasOfOne_Accepts()
  {
    var baker = new Baker("Mario", 1, PreparationTime);

    Assert.Equal(1, baker.MaxConcurrentPizzas);
  }

  #endregion

  #region Tempo di preparazione

  [Theory]
  [MemberData(nameof(ZeroOrNegativeDurations))]
  public void Constructor_WithZeroOrNegativePreparationTime_Throws(TimeSpan preparationTime)
  {
    Assert.Throws<DomainException>(() => new Baker("Mario", 3, preparationTime));
  }

  #endregion
}
