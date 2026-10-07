using Api.Errors;
using Application;
using Application.Planning;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("v1", options =>
{
  options.AddDocumentTransformer((document, context, cancellationToken) =>
  {
    document.Info.Title = "Awesome Pizza API";
    document.Info.Version = "v1";
    return Task.CompletedTask;
  });
});

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
var planning = builder.Configuration.GetSection(PlanningOptions.SectionName).Get<PlanningOptions>() ?? new PlanningOptions();
builder.Services.AddApplication(planning);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  using (var scope = app.Services.CreateScope())
  {
    await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
  }

  app.MapOpenApi();
  app.UseSwaggerUI(options =>
  {
    options.SwaggerEndpoint("/openapi/v1.json", "Awesome Pizza API v1");
  });
}

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.MapControllers();

app.Run();
