using Application.Kitchen;
using Microsoft.Extensions.Options;

namespace Api.Workers;

public sealed class BatchAssignmentWorker(
  IServiceScopeFactory scopes,
  IOptions<BatchAssignmentOptions> options,
  TimeProvider time,
  ILogger<BatchAssignmentWorker> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds), time);

    do
    {
      await AssignAsync(stoppingToken);
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
  }

  private async Task AssignAsync(CancellationToken ct)
  {
    try
    {
      await using var scope = scopes.CreateAsyncScope();
      var kitchen = scope.ServiceProvider.GetRequiredService<IKitchenService>();

      var assigned = await kitchen.AssignDueBatchesAsync(ct);
      if (assigned > 0)
        logger.LogInformation("Assigned {Count} batch(es) to the baker.", assigned);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      logger.LogError(ex, "Batch assignment failed; retrying at the next tick.");
    }
  }
}
