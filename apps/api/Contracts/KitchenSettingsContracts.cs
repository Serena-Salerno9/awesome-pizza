using System.ComponentModel.DataAnnotations;
using Api.Workers;
using Application.Kitchen;
using Application.Planning;

namespace Api.Contracts;

public sealed record KitchenSettingsRequest(
  [Range(1, 100)] int MaxConcurrentPizzas,
  [Range(0.5, 120)] double PreparationMinutes,
  [Range(1, 100)] int OvenCapacity,
  [Range(0.5, 120)] double BakingMinutes)
{
  public KitchenSettings ToSettings() =>
    new(MaxConcurrentPizzas, TimeSpan.FromMinutes(PreparationMinutes), OvenCapacity, TimeSpan.FromMinutes(BakingMinutes));
}

public sealed record SystemSettingsResponse(PlanningPolicy PlanningPolicy, int BackfillToleranceMinutes, int AssignmentIntervalSeconds);

public sealed record KitchenSettingsResponse(
  int MaxConcurrentPizzas,
  double PreparationMinutes,
  int OvenCapacity,
  double BakingMinutes,
  SystemSettingsResponse System)
{
  public static KitchenSettingsResponse From(KitchenSettings settings, PlanningOptions planning, BatchAssignmentOptions assignment) =>
    new(
      settings.MaxConcurrentPizzas,
      settings.PreparationTimePerPizza.TotalMinutes,
      settings.OvenCapacity,
      settings.BakingTime.TotalMinutes,
      new SystemSettingsResponse(planning.Policy, planning.BackfillToleranceMinutes, assignment.IntervalSeconds));
}
