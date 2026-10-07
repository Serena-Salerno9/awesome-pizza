using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Catalog;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Domain.Orders;

public class OrderLine
{
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  public Guid FkOrder { get; private set; }

  [ForeignKey(nameof(FkOrder))]
  public virtual Order FkOrderNavigation { get; private set; } = null!;

  [MaxLength(Pizza.NameMaxLength)]
  public string PizzaName { get; private set; } = null!;

  [Precision(10, Pizza.PriceDecimals)]
  public decimal UnitPrice { get; private set; }

  public int Quantity { get; private set; }

  [NotMapped]
  public decimal LineTotal => UnitPrice * Quantity;

  private OrderLine() { }

  internal OrderLine(Guid orderId, string pizzaName, decimal unitPrice, int quantity)
  {
    if (string.IsNullOrWhiteSpace(pizzaName))
      throw new DomainException("Pizza name is required.");

    if (unitPrice < 0)
      throw new DomainException("Unit price cannot be negative.");

    if (quantity < 1)
      throw new DomainException("Quantity must be at least 1.");

    Id = Guid.NewGuid();
    FkOrder = orderId;
    PizzaName = pizzaName;
    UnitPrice = unitPrice;
    Quantity = quantity;
  }
}
