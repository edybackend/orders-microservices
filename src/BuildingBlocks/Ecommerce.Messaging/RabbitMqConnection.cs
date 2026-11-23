using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Ecommerce.Messaging;

/// <summary>
/// Owns the single long-lived AMQP connection of the process (connections are expensive; channels are cheap).
/// Retries on startup so a service can boot before the broker is ready.
/// </summary>
public sealed partial class RabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IChannel> CreateChannelAsync(CreateChannelOptions? channelOptions, CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        await RabbitMqTopology.DeclareExchangesAsync(channel, _options, cancellationToken);
        return channel;
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory { Uri = new Uri(_options.ConnectionString) };
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(cancellationToken);
                    LogConnected(factory.Endpoint);
                    return _connection;
                }
                catch (BrokerUnreachableException ex) when (attempt < _options.ConnectRetryCount)
                {
                    LogRetry(attempt, _options.ConnectRetryCount, ex.Message);
                    await Task.Delay(_options.ConnectRetryDelay, cancellationToken);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _lock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to RabbitMQ at {Endpoint}")]
    private partial void LogConnected(AmqpTcpEndpoint endpoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RabbitMQ not reachable (attempt {Attempt}/{Max}): {Reason}")]
    private partial void LogRetry(int attempt, int max, string reason);
}
