using Domain.Exceptions;
using Domain.Kitchen;

namespace Domain.Tests.Kitchen;

public class OvenTests
{
  // Gli attributi C# non accettano TimeSpan, MemberData permette di testare valori reali
  public static TheoryData<TimeSpan> ZeroOrNegativeDurations => [TimeSpan.Zero, TimeSpan.FromSeconds(-1), TimeSpan.FromMinutes(-5)];

  #region Costruttore

  [Fact]
  public void Constructor_WithValidData_CreatesOven()
  {
    var oven = new Oven(4, TimeSpan.FromMinutes(8));

    Assert.NotEqual(Guid.Empty, oven.Id);
    Assert.Equal(4, oven.Capacity);
    Assert.Equal(TimeSpan.FromMinutes(8), oven.BakingTime);
  }

  #endregion

  #region Capienza

  [Theory]
  [InlineData(0)]
  [InlineData(-1)]
  public void Constructor_WithCapacityBelowOne_Throws(int capacity)
  {
    Assert.Throws<DomainException>(() => new Oven(capacity, TimeSpan.FromMinutes(8)));
  }

  [Fact]
  public void Constructor_WithCapacityOfOne_Accepts()
  {
    var oven = new Oven(1, TimeSpan.FromMinutes(8));

    Assert.Equal(1, oven.Capacity);
  }

  #endregion

  #region Tempo di cottura

  [Theory]
  [MemberData(nameof(ZeroOrNegativeDurations))]
  public void Constructor_WithZeroOrNegativeBakingTime_Throws(TimeSpan bakingTime)
  {
    Assert.Throws<DomainException>(() => new Oven(4, bakingTime));
  }

  #endregion
}
