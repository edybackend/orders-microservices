using Ecommerce.Contracts;
using Ecommerce.Messaging;
using Payments.Worker.Application;
using Payments.Worker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Payments") ?? "Data Source=data/payments.db";
var repository = new DapperPaymentRepository(connectionString);

builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPaymentRepository>(repository);
builder.Services.AddScoped<IIntegrationEventConsumer<OrderPlaced>, OrderPlacedConsumer>();
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddRabbitMqConsumer("payments.order-placed", subscription => subscription
    .On<OrderPlaced>(RoutingKeys.OrderPlaced));

var host = builder.Build();
await repository.InitializeAsync(CancellationToken.None);
await host.RunAsync();
