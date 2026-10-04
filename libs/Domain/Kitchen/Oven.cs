using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Exceptions;

namespace Domain.Kitchen;

public class Oven
{
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  public int Capacity { get; private set; }

  public TimeSpan BakingTime { get; private set; }

  private Oven() { }

  public Oven(int capacity, TimeSpan bakingTime)
  {
    if (capacity < 1)
      throw new DomainException("Oven capacity must be at least 1.");

    if (bakingTime <= TimeSpan.Zero)
      throw new DomainException("Baking time must be greater than zero.");

    Id = Guid.NewGuid();
    Capacity = capacity;
    BakingTime = bakingTime;
  }
}
