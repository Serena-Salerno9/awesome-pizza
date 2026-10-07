using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Exceptions;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Domain.Kitchen;

[Index(nameof(FkWorkstation), nameof(ReadyAt))]
public class Batch
{
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  public Guid FkWorkstation { get; private set; }

  [ForeignKey(nameof(FkWorkstation))]
  [DeleteBehavior(DeleteBehavior.Restrict)]
  public virtual Workstation FkWorkstationNavigation { get; private set; } = null!;

  public DateTimeOffset StartedAt { get; private set; }
  public DateTimeOffset? TakenAt { get; private set; }
  public DateTimeOffset? ReadyAt { get; private set; }

  [InverseProperty(nameof(BatchLine.FkBatchNavigation))]
  public virtual ICollection<BatchLine> Lines { get; private set; } = [];
  [NotMapped]
  public int PizzaCount => Lines.Sum(l => l.Quantity);

  [ConcurrencyCheck]
  public Guid Version { get; private set; }

  private Batch() { }

  public Batch(Guid workstationId, IEnumerable<(OrderLine Line, int Quantity)> items, DateTimeOffset startedAt)
  {
    ArgumentNullException.ThrowIfNull(items);

    if (workstationId == Guid.Empty)
      throw new DomainException("Workstation is required.");

    Id = Guid.NewGuid();
    FkWorkstation = workstationId;
    StartedAt = startedAt;
    Version = Guid.NewGuid();

    foreach (var (line, quantity) in items)
      AddLine(line, quantity);

    if (Lines.Count == 0)
      throw new DomainException("A batch must contain at least one pizza.");
  }

  public void TakeCharge(DateTimeOffset now)
  {
    if (ReadyAt is not null)
      throw new DomainException("A ready batch cannot be taken in charge.");

    if (TakenAt is not null)
      throw new DomainException("Batch has already been taken in charge.");

    if (now < StartedAt)
      throw new DomainException("A batch cannot be taken in charge before it started.");

    TakenAt = now;
    Version = Guid.NewGuid();
  }

  public void MarkReady(DateTimeOffset now)
  {
    if (ReadyAt is not null)
      throw new DomainException("Batch is already ready.");

    if (now < (TakenAt ?? StartedAt))
      throw new DomainException("A batch cannot be ready before it started.");

    ReadyAt = now;
    Version = Guid.NewGuid();
  }

  private void AddLine(OrderLine line, int quantity)
  {
    if (line is null)
      throw new DomainException("Order line is required.");

    if (quantity > line.Quantity)
      throw new DomainException("A batch cannot contain more pizzas than the order line.");

    if (Lines.Any(l => l.FkOrderLine == line.Id))
      throw new DomainException("An order line can appear only once in a batch.");

    Lines.Add(new BatchLine(Id, line.Id, quantity));
  }
}
