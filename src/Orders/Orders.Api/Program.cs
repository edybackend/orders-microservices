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

// Malformed JSON raises BadHttpRequestException, which carries its own 400.
// Without the selector the exception handler would turn it into a 500 in Development.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapOrderEndpoints();
// Unknown /api routes are 404 problems; every other unknown path is a UI route and gets index.html.
app.MapFallback("/api/{**path}", () => TypedResults.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

await app.RunAsync();

/// <summary>Entry point, exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
