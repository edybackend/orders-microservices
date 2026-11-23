using Ecommerce.Contracts;
using Microsoft.Extensions.Options;
using Payments.Worker.Application;
using Payments.Worker.Infrastructure;

namespace Payments.UnitTests;

public sealed class OrderPlacedConsumerTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"payments-tests-{Guid.NewGuid():N}.db");
    private readonly RecordingPublisher _publisher = new();
    private DapperPaymentRepository _repository = null!;
    private OrderPlacedConsumer _consumer = null!;

    public async ValueTask InitializeAsync()
    {
        _repository = new DapperPaymentRepository($"Data Source={_databasePath};Pooling=False");
        await _repository.InitializeAsync(TestContext.Current.CancellationToken);
        var options = Options.Create(new PaymentOptions { ApprovalLimit = 5000m, SimulatedLatency = TimeSpan.Zero });
        _consumer = new OrderPlacedConsumer(_repository, _publisher, options, TimeProvider.System);
    }

    public ValueTask DisposeAsync()
    {
        File.Delete(_databasePath);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Amount_within_limit_publishes_PaymentApproved()
    {
        var order = NewOrder(299.80m);

        await _consumer.ConsumeAsync(order, TestContext.Current.CancellationToken);

        var approved = Assert.IsType<PaymentApproved>(Assert.Single(_publisher.Published).Event);
        Assert.Equal(order.OrderId, approved.OrderId);
        Assert.Equal(299.80m, approved.Amount);
    }

    [Fact]
    public async Task Amount_above_limit_publishes_PaymentRejected()
    {
        await _consumer.ConsumeAsync(NewOrder(7999m), TestContext.Current.CancellationToken);

        var (published, routingKey) = Assert.Single(_publisher.Published);
        Assert.IsType<PaymentRejected>(published);
        Assert.Equal(RoutingKeys.PaymentRejected, routingKey);
    }

    [Fact]
    public async Task Redelivered_order_is_charged_once_and_republishes_the_same_event()
    {
        var order = NewOrder(100m);

        await _consumer.ConsumeAsync(order, TestContext.Current.CancellationToken);
        await _consumer.ConsumeAsync(order, TestContext.Current.CancellationToken);

        Assert.Equal(2, _publisher.Published.Count);
        Assert.Equal(_publisher.Published[0].Event.EventId, _publisher.Published[1].Event.EventId);
        var stored = await _repository.FindByOrderIdAsync(order.OrderId, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
    }

    private static OrderPlaced NewOrder(decimal total) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), "ana@example.com", total);

    private sealed class RecordingPublisher : IEventPublisher
    {
        public List<(IIntegrationEvent Event, string RoutingKey)> Published { get; } = [];

        public Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken)
            where TEvent : IIntegrationEvent
        {
            Published.Add((integrationEvent, routingKey));
            return Task.CompletedTask;
        }
    }
}
