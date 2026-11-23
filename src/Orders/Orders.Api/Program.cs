using System.Text.Json.Serialization;
using Orders.Api.Endpoints;
using Orders.Api.Realtime;
using Orders.Application;
using Orders.Application.Abstractions;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrdersApplication();
builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddSingleton<OrderStatusBroadcaster>();
builder.Services.AddSingleton<IOrderStatusNotifier>(sp => sp.GetRequiredService<OrderStatusBroadcaster>());
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

await app.Services.MigrateOrdersDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapOrderEndpoints();
app.MapFallbackToFile("index.html");

await app.RunAsync();

/// <summary>Entry point, exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
