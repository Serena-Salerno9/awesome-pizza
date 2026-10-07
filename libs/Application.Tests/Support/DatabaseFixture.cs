using Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Tests.Support;

public sealed class DatabaseFixture : IAsyncLifetime
{
  private const string TestDatabaseName = "AwesomePizza_Tests";
  private const string ApiUserSecretsId = "be8280cc-0c68-4ea3-84cd-7ea4fd9f66af";

  private readonly DbContextOptions<AppDbContext> _options =
    new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ResolveConnectionString()).Options;

  public AppDbContext CreateContext() => new(_options);

  public async Task InitializeAsync()
  {
    await using var db = CreateContext();
    await db.Database.MigrateAsync();
  }

  public Task DisposeAsync() => Task.CompletedTask;

  public async Task ResetAsync()
  {
    await using var db = CreateContext();

    await db.BatchLines.ExecuteDeleteAsync();
    await db.Batches.ExecuteDeleteAsync();
    await db.OrderLines.ExecuteDeleteAsync();
    await db.Orders.ExecuteDeleteAsync();
    await db.Workstations.ExecuteDeleteAsync();
    await db.Bakers.ExecuteDeleteAsync();
    await db.Ovens.ExecuteDeleteAsync();
    await db.Pizzas.ExecuteDeleteAsync();

    await DbSeeder.SeedAsync(db);
  }

  private static string ResolveConnectionString()
  {
    var configuration = new ConfigurationBuilder()
      .AddUserSecrets(ApiUserSecretsId)
      .AddEnvironmentVariables()
      .Build();

    var connectionString = configuration["ConnectionStrings:Tests"];
    if (connectionString is null)
    {
      var development = configuration["ConnectionStrings:Default"]
        ?? throw new InvalidOperationException(
          "Test database not configured: set the API user secret 'ConnectionStrings:Default' (see README) or the environment variable 'ConnectionStrings__Tests'.");

      connectionString = new SqlConnectionStringBuilder(development) { InitialCatalog = TestDatabaseName }.ConnectionString;
    }

    var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
    if (!database.EndsWith("_Tests", StringComparison.OrdinalIgnoreCase))
      throw new InvalidOperationException($"Refusing to run tests on database '{database}': its name must end with '_Tests', because tests delete all data.");

    return connectionString;
  }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
  public const string Name = "Database";
}
