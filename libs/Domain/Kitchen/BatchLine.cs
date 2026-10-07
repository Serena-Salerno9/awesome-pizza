using System;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Exceptions;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Domain.Kitchen;

public class BatchLine
{
  [DatabaseGenerated(DatabaseGeneratedOption.None)]
  public Guid Id { get; private set; }

  public Guid FkBatch { get; private set; }

  [ForeignKey(nameof(FkBatch))]
  public virtual Batch FkBatchNavigation { get; private set; } = null!;

  public Guid FkOrderLine { get; private set; }

  [ForeignKey(nameof(FkOrderLine))]
  [DeleteBehavior(DeleteBehavior.Restrict)]
  public virtual OrderLine FkOrderLineNavigation { get; private set; } = null!;

  public int Quantity { get; private set; }

  private BatchLine() { }

  internal BatchLine(Guid batchId, Guid orderLineId, int quantity)
  {
    if (quantity < 1)
      throw new DomainException("Quantity must be at least 1.");

    Id = Guid.NewGuid();
    FkBatch = batchId;
    FkOrderLine = orderLineId;
    Quantity = quantity;
  }
}
