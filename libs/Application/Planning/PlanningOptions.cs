using System;

namespace Application.Planning;

public enum PlanningPolicy
{
  Fifo,
  Backfill
}

public class PlanningOptions
{
  public const string SectionName = "Planning";

  public PlanningPolicy Policy { get; set; } = PlanningPolicy.Backfill;
  public int BackfillToleranceMinutes { get; set; } = 2;
}
