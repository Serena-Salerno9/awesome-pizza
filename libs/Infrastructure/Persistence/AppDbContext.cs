using System;
using Domain.Catalog;
using Domain.Kitchen;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Pizza> Pizzas => Set<Pizza>();
  public DbSet<Baker> Bakers => Set<Baker>();
  public DbSet<Oven> Ovens => Set<Oven>();
  public DbSet<Workstation> Workstations => Set<Workstation>();
  public DbSet<Order> Orders => Set<Order>();
  public DbSet<OrderLine> OrderLines => Set<OrderLine>();
}
