using Domain.Exceptions;
using Domain.Kitchen;

namespace Domain.Tests.Kitchen;

public class WorkstationTests
{
  private static readonly Guid BakerId = Guid.NewGuid();
  private static readonly Guid OvenId = Guid.NewGuid();

  #region Costruttore

  [Fact]
  public void Constructor_WithValidData_CreatesWorkstation()
  {
    var workstation = new Workstation("Postazione 1", BakerId, OvenId);

    Assert.NotEqual(Guid.Empty, workstation.Id);
    Assert.Equal("Postazione 1", workstation.Name);
    Assert.Equal(BakerId, workstation.FkBaker);
    Assert.Equal(OvenId, workstation.FkOven);
  }

  #endregion

  #region Nome

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Constructor_WithMissingName_Throws(string? name)
  {
    Assert.Throws<DomainException>(() => new Workstation(name!, BakerId, OvenId));
  }

  [Fact]
  public void Constructor_TrimsName()
  {
    var workstation = new Workstation("  Postazione 1  ", BakerId, OvenId);

    Assert.Equal("Postazione 1", workstation.Name);
  }

  [Fact]
  public void Constructor_WithNameAtMaxLengthAfterTrim_Accepts()
  {
    var name = "  " + new string('a', Workstation.NameMaxLength) + "  ";

    var workstation = new Workstation(name, BakerId, OvenId);

    Assert.Equal(Workstation.NameMaxLength, workstation.Name.Length);
  }

  [Fact]
  public void Constructor_WithNameTooLong_Throws()
  {
    var name = new string('a', Workstation.NameMaxLength + 1);

    Assert.Throws<DomainException>(() => new Workstation(name, BakerId, OvenId));
  }

  #endregion

  #region Pizzaiolo e forno

  [Fact]
  public void Constructor_WithEmptyBaker_Throws()
  {
    Assert.Throws<DomainException>(() => new Workstation("Postazione 1", Guid.Empty, OvenId));
  }

  [Fact]
  public void Constructor_WithEmptyOven_Throws()
  {
    Assert.Throws<DomainException>(() => new Workstation("Postazione 1", BakerId, Guid.Empty));
  }

  [Fact]
  public void AssignBaker_WithValidBaker_ReplacesBaker()
  {
    var workstation = new Workstation("Postazione 1", BakerId, OvenId);
    var newBakerId = Guid.NewGuid();

    workstation.AssignBaker(newBakerId);

    Assert.Equal(newBakerId, workstation.FkBaker);
    Assert.Equal(OvenId, workstation.FkOven);
  }

  [Fact]
  public void AssignBaker_WithEmptyBaker_ThrowsAndKeepsCurrentBaker()
  {
    var workstation = new Workstation("Postazione 1", BakerId, OvenId);

    Assert.Throws<DomainException>(() => workstation.AssignBaker(Guid.Empty));

    Assert.Equal(BakerId, workstation.FkBaker);
  }

  #endregion
}
