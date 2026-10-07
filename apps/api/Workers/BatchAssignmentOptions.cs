using System.ComponentModel.DataAnnotations;

namespace Api.Workers;

public sealed class BatchAssignmentOptions
{
  public const string SectionName = "BatchAssignment";

  [Range(1, 3600)]
  public int IntervalSeconds { get; set; } = 5;
}
