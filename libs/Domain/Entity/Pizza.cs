using System;
using Exceptions;

namespace Domain;

public class Pizza
{
  public const int NameMaxLength = 100;
  public const int DescriptionMaxLength = 500;

  public Guid Id { get; private set; }
  public string Name { get; private set; }
  public string? Description { get; private set; }
  public decimal Price { get; private set; }
  public bool IsOnMenu { get; private set; }

  public Pizza(string name, string? description, decimal price)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException("Pizza name is required.");
    if (name.Trim().Length > NameMaxLength)
      throw new DomainException($"Pizza name cannot exceed {NameMaxLength} characters.");
    if (description?.Length > DescriptionMaxLength)
      throw new DomainException($"Pizza description cannot exceed {DescriptionMaxLength} characters.");
    if (price <= 0)
      throw new DomainException("Pizza price must be greater than zero.");

    Id = Guid.NewGuid();
    Name = name.Trim();
    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    Price = price;
    IsOnMenu = true;
  }
}
