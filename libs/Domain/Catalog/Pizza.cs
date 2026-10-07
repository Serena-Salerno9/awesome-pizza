using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Domain.Catalog;

public class Pizza
{
  public const int NameMaxLength = 100;
  public const int DescriptionMaxLength = 500;
  public const int PriceDecimals = 2;

  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  [MaxLength(NameMaxLength)]
  public string Name { get; private set; } = null!;

  [MaxLength(DescriptionMaxLength)]
  public string? Description { get; private set; }

  [Precision(10, PriceDecimals)]
  public decimal Price { get; private set; }

  public bool IsOnMenu { get; private set; }

  private Pizza() { }

  public Pizza(string name, string? description, decimal price)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new DomainException("Pizza name is required.");

    var trimmedName = name.Trim();
    if (trimmedName.Length > NameMaxLength)
      throw new DomainException($"Pizza name cannot exceed {NameMaxLength} characters.");

    var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    if (trimmedDescription?.Length > DescriptionMaxLength)
      throw new DomainException($"Pizza description cannot exceed {DescriptionMaxLength} characters.");

    if (price <= 0)
      throw new DomainException("Pizza price must be greater than zero.");

    if (decimal.Round(price, PriceDecimals) != price)
      throw new DomainException($"Pizza price cannot have more than {PriceDecimals} decimal places.");

    Id = Guid.NewGuid();
    Name = trimmedName;
    Description = trimmedDescription;
    Price = price;
    IsOnMenu = true;
  }

  public void RemoveFromMenu() => IsOnMenu = false;

  public void AddBackToMenu() => IsOnMenu = true;
}
