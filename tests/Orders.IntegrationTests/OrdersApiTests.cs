using System.Net;
using System.Net.Http.Json;
using Ecommerce.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.IntegrationTests;

public class OrdersApiTests(OrdersApiFactory factory) : IClassFixture<OrdersApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object ValidOrder(string email = "ana@example.com") => new
    {
        customerEmail = email,
        items = new[] { new { productName = "Keyboard", quantity = 2, unitPrice = 149.90m } },
    };

    [Fact]
    public async Task Post_creates_a_pending_order_readable_through_the_query_side()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrder(), ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedOrder>(ct);
        Assert.NotNull(response.Headers.Location);

        var order = await _client.GetFromJsonAsync<OrderView>(response.Headers.Location, ct);
        Assert.NotNull(order);
        Assert.Equal(created!.Id, order.Id);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(299.80m, order.Total);
        Assert.Equal("Keyboard", Assert.Single(order.Items).ProductName);
        Assert.Contains(factory.Publisher.Published, e => e is OrderPlaced placed && placed.OrderId == created.Id);
    }

    [Fact]
    public async Task Post_with_invalid_email_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrder("nope"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_unknown_order_returns_404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Payment_result_updates_the_order_status()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await (await _client.PostAsJsonAsync("/api/orders", ValidOrder(), ct)).Content.ReadFromJsonAsync<CreatedOrder>(ct);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer<PaymentRejected>>();
            await consumer.ConsumeAsync(new PaymentRejected(Guid.NewGuid(), DateTimeOffset.UtcNow, created!.Id, "Limit exceeded"), ct);
        }

        var order = await _client.GetFromJsonAsync<OrderView>($"/api/orders/{created.Id}", ct);
        Assert.Equal("Rejected", order!.Status);
        Assert.Equal("Limit exceeded", order.RejectionReason);
    }

    [Fact]
    public async Task Malformed_json_returns_400_not_500()
    {
        using var body = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/orders", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_api_route_is_404_not_the_spa_page()
    {
        var response = await _client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record CreatedOrder(Guid Id);

    private sealed record OrderView(Guid Id, string Status, decimal Total, string? RejectionReason, IReadOnlyList<OrderLineView> Items);

    private sealed record OrderLineView(string ProductName, int Quantity, decimal UnitPrice);
}
