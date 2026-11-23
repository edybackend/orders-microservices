using Microsoft.AspNetCore.Http.HttpResults;
using Orders.Api.Realtime;
using Orders.Application.PlaceOrder;
using Orders.Application.Queries;

namespace Orders.Api.Endpoints;

internal static class OrderEndpoints
{
    private const int RecentOrdersLimit = 50;

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/api/orders").WithTags("Orders");

        orders.MapPost("/", PlaceOrderAsync).WithName("PlaceOrder");
        orders.MapGet("/", ListOrdersAsync).WithName("ListOrders");
        orders.MapGet("/{id:guid}", GetOrderAsync).WithName("GetOrder");
        orders.MapGet("/events", (OrderStatusBroadcaster broadcaster, CancellationToken ct) =>
            TypedResults.ServerSentEvents(broadcaster.SubscribeAsync(ct), eventType: "order-status"))
            .WithName("OrderStatusStream");

        return app;
    }

    private static async Task<Results<CreatedAtRoute<PlaceOrderResponse>, ValidationProblem>> PlaceOrderAsync(
        PlaceOrderCommand command, PlaceOrderHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(new PlaceOrderResponse(result.Value), "GetOrder", new { id = result.Value })
            : TypedResults.ValidationProblem(result.Errors!);
    }

    private static async Task<Ok<IReadOnlyList<OrderSummary>>> ListOrdersAsync(IOrderQueries queries, CancellationToken ct) =>
        TypedResults.Ok(await queries.ListRecentAsync(RecentOrdersLimit, ct));

    private static async Task<Results<Ok<OrderDetails>, NotFound>> GetOrderAsync(Guid id, IOrderQueries queries, CancellationToken ct) =>
        await queries.GetByIdAsync(id, ct) is { } order ? TypedResults.Ok(order) : TypedResults.NotFound();
}

internal sealed record PlaceOrderResponse(Guid Id);
