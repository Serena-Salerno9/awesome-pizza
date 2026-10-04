using Domain.Exceptions;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Domain.Kitchen;

public class Baker
{
  public const int NameMaxLength = 100;

  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  [MaxLength(NameMaxLength)]
  public string Name { get; private set; } = null!;

  public int MaxConcurrentPizzas { get; private set; }

  public TimeSpan PreparationTimePerPizza { get; private set; }

  private Baker() { }

  public Baker(string name, int maxConcurrentPizzas, TimeSpan preparationTimePerPizza)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException("Baker name is required.");

    var trimmedName = name.Trim();
    if (trimmedName.Length > NameMaxLength)
      throw new DomainException($"Baker name cannot exceed {NameMaxLength} characters.");

    if (maxConcurrentPizzas < 1)
      throw new DomainException("A baker must handle at least one pizza at a time.");

    if (preparationTimePerPizza <= TimeSpan.Zero)
      throw new DomainException("Preparation time must be greater than zero.");

    Id = Guid.NewGuid();
    Name = trimmedName;
    MaxConcurrentPizzas = maxConcurrentPizzas;
    PreparationTimePerPizza = preparationTimePerPizza;
  }
}
