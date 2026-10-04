using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Catalog;
using Domain.Exceptions;
using Domain.Kitchen;
using Microsoft.EntityFrameworkCore;

namespace Domain.Orders;

[Index(nameof(BusinessDate), nameof(DailyNumber), IsUnique = true)]
[Index(nameof(Status), nameof(BusinessDate), nameof(DailyNumber))]
public class Order
{
  public const int MaxPizzasPerOrder = 20;
  public const int MaxDailyOrders = 999;
  private static readonly string CodeFormat = $"D{MaxDailyOrders.ToString().Length}";

  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }
  public DateOnly BusinessDate { get; private set; }
  public int DailyNumber { get; private set; }
  [NotMapped]
  public string Code => DailyNumber.ToString(CodeFormat);

  [Column(TypeName = "nvarchar(20)")]
  public OrderStatus Status { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }
  public DateTimeOffset? StartedAt { get; private set; }
  public DateTimeOffset? ReadyAt { get; private set; }

  public Guid? FkWorkstation { get; private set; }
  [ForeignKey(nameof(FkWorkstation))]
  [DeleteBehavior(DeleteBehavior.Restrict)]
  public virtual Workstation? FkWorkstationNavigation { get; private set; }

  [InverseProperty(nameof(OrderLine.FkOrderNavigation))]
  public virtual ICollection<OrderLine> Lines { get; private set; } = [];
  [NotMapped]
  public int PizzaCount => Lines.Sum(l => l.Quantity);
  [NotMapped]
  public decimal Total => Lines.Sum(l => l.LineTotal);

  [ConcurrencyCheck]
  public Guid Version { get; private set; }

  private Order() { }

  public Order(
    DateOnly businessDate,
    int dailyNumber,
    IEnumerable<(Pizza Pizza, int Quantity)> items,
    DateTimeOffset now)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(dailyNumber, 1);
    ArgumentNullException.ThrowIfNull(items);

    if (dailyNumber > MaxDailyOrders)
      throw new DomainException($"Daily order limit of {MaxDailyOrders} exceeded.");

    Id = Guid.NewGuid();
    BusinessDate = businessDate;
    DailyNumber = dailyNumber;
    Status = OrderStatus.Queued;
    CreatedAt = now;
    Version = Guid.NewGuid();

    foreach (var (pizza, quantity) in items)
      AddLine(pizza, quantity);

    if (Lines.Count == 0)
      throw new DomainException("An order must contain at least one pizza.");

    if (PizzaCount > MaxPizzasPerOrder)
      throw new DomainException($"An order cannot contain more than {MaxPizzasPerOrder} pizzas.");
  }

  public void StartPreparation(Guid workstationId, DateTimeOffset now)
  {
    if (workstationId == Guid.Empty)
      throw new DomainException("Workstation is required.");

    TransitionTo(OrderStatus.InPreparation);
    FkWorkstation = workstationId;
    StartedAt = now;
  }

  public void MarkReady(DateTimeOffset now)
  {
    TransitionTo(OrderStatus.Ready);
    ReadyAt = now;
  }

  private void AddLine(Pizza pizza, int quantity)
  {
    if (pizza is null)
      throw new DomainException("Pizza is required.");

    if (!pizza.IsOnMenu)
      throw new DomainException($"Pizza '{pizza.Name}' is not currently on the menu.");

    Lines.Add(new OrderLine(Id, pizza.Name, pizza.Price, quantity));
  }

  private void TransitionTo(OrderStatus target)
  {
    OrderStateMachine.EnsureCanTransition(Status, target);
    Status = target;
    Version = Guid.NewGuid();
  }
}
