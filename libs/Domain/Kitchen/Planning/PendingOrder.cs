using System;

namespace Domain.Kitchen.Planning;

public sealed record PendingLine(Guid OrderLineId, int Quantity);

public sealed record PendingOrder(Guid OrderId, IReadOnlyList<PendingLine> Lines);
