using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Domain.Kitchen;

[Index(nameof(FkBaker), IsUnique = true)]
[Index(nameof(FkOven), IsUnique = true)]
public class Workstation
{
  public const int NameMaxLength = 100;

  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  [MaxLength(NameMaxLength)]
  public string Name { get; private set; } = null!;

  public Guid FkBaker { get; private set; }

  [ForeignKey(nameof(FkBaker))]
  [DeleteBehavior(DeleteBehavior.Restrict)]
  public virtual Baker FkBakerNavigation { get; private set; } = null!;

  public Guid FkOven { get; private set; }

  [ForeignKey(nameof(FkOven))]
  [DeleteBehavior(DeleteBehavior.Restrict)]
  public virtual Oven FkOvenNavigation { get; private set; } = null!;

  private Workstation() { }

  public Workstation(string name, Guid bakerId, Guid ovenId)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException("Workstation name is required.");

    var trimmedName = name.Trim();
    if (trimmedName.Length > NameMaxLength)
      throw new DomainException($"Workstation name cannot exceed {NameMaxLength} characters.");

    if (bakerId == Guid.Empty)
      throw new DomainException("Baker is required.");

    if (ovenId == Guid.Empty)
      throw new DomainException("Oven is required.");

    Id = Guid.NewGuid();
    Name = trimmedName;
    FkBaker = bakerId;
    FkOven = ovenId;
  }

  public void AssignBaker(Guid bakerId)
  {
    if (bakerId == Guid.Empty)
      throw new DomainException("Baker is required.");

    FkBaker = bakerId;
  }
}
