using Domain.Exceptions;
using Domain.Orders;

namespace Domain.Tests.Orders;

public class OrderStateMachineTests
{
  public static TheoryData<OrderStatus, OrderStatus> AllowedTransitions => new()
  {
    { OrderStatus.Queued, OrderStatus.InPreparation },
    { OrderStatus.InPreparation, OrderStatus.Ready }
  };

  public static TheoryData<OrderStatus, OrderStatus> ForbiddenTransitions => new()
  {
    { OrderStatus.Queued, OrderStatus.Queued },
    { OrderStatus.Queued, OrderStatus.Ready },
    { OrderStatus.InPreparation, OrderStatus.Queued },
    { OrderStatus.InPreparation, OrderStatus.InPreparation },
    { OrderStatus.Ready, OrderStatus.Queued },
    { OrderStatus.Ready, OrderStatus.InPreparation },
    { OrderStatus.Ready, OrderStatus.Ready }
  };

  #region CanTransition

  [Theory]
  [MemberData(nameof(AllowedTransitions))]
  public void CanTransition_WithAllowedTransition_ReturnsTrue(OrderStatus from, OrderStatus to)
  {
    Assert.True(OrderStateMachine.CanTransition(from, to));
  }

  [Theory]
  [MemberData(nameof(ForbiddenTransitions))]
  public void CanTransition_WithForbiddenTransition_ReturnsFalse(OrderStatus from, OrderStatus to)
  {
    Assert.False(OrderStateMachine.CanTransition(from, to));
  }

  #endregion

  #region EnsureCanTransition

  [Theory]
  [MemberData(nameof(AllowedTransitions))]
  public void EnsureCanTransition_WithAllowedTransition_DoesNotThrow(OrderStatus from, OrderStatus to)
  {
    var exception = Record.Exception(() => OrderStateMachine.EnsureCanTransition(from, to));

    Assert.Null(exception);
  }

  [Theory]
  [MemberData(nameof(ForbiddenTransitions))]
  public void EnsureCanTransition_WithForbiddenTransition_Throws(OrderStatus from, OrderStatus to)
  {
    Assert.Throws<DomainException>(() => OrderStateMachine.EnsureCanTransition(from, to));
  }

  #endregion
}
