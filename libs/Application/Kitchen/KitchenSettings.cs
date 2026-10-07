using System;

namespace Application.Kitchen;

public sealed record KitchenSettings(
  int MaxConcurrentPizzas,
  TimeSpan PreparationTimePerPizza,
  int OvenCapacity,
  TimeSpan BakingTime);
