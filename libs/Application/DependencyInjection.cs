using System;
using Application.Kitchen;
using Application.Menu;
using Application.Orders;
using Application.Planning;
using Domain.Kitchen.Planning;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services, PlanningOptions planning)
  {
    ArgumentNullException.ThrowIfNull(planning);

    services.AddSingleton(CreatePlanner(planning));
    services.AddSingleton(TimeProvider.System);
    services.AddSingleton<KitchenPlanBuilder>();
    services.AddScoped<KitchenPlanner>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IKitchenService, KitchenService>();
    services.AddScoped<IMenuService, MenuService>();

    return services;
  }

  private static IBatchPlanner CreatePlanner(PlanningOptions planning) => planning.Policy switch
  {
    PlanningPolicy.Fifo => new FifoBatchPlanner(),
    PlanningPolicy.Backfill => new BackfillBatchPlanner(TimeSpan.FromMinutes(planning.BackfillToleranceMinutes)),
    _ => throw new InvalidOperationException($"Unknown planning policy '{planning.Policy}'.")
  };
}
