using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using WatchlistService.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace WatchlistService.Infrastructure.RabbitMQ;

public class RabbitMQConsumer : IAsyncDisposable
{
    private readonly string _hostname = "localhost";
    private readonly int _port = 5672;
    private readonly string _queueName = "WatchlistQueue";
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed = false;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMQConsumer> _logger;

    public RabbitMQConsumer(IServiceScopeFactory scopeFactory, ILogger<RabbitMQConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartListeningAsync()
    {
        var factory = new ConnectionFactory { HostName = _hostname, Port = _port };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(
            queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventObj = JsonSerializer.Deserialize<UserCreatedEvent>(message);

                if (eventObj is not null && eventObj.EventType == "UserCreated")
                {
                    // Create a new scope per message
                    using var scope = _scopeFactory.CreateScope();
                    var watchlistService = scope.ServiceProvider.GetRequiredService<IWatchlistService>();

                    await watchlistService.CreateWatchlistAsync(eventObj.UserId);
                    _logger.LogInformation("[Watchlist] Created watchlist for user {UserId}", eventObj.UserId);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message");
                if (_channel != null)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            }
        };


        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_channel != null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }

            _disposed = true;
        }
    }
}

public class UserCreatedEvent
{
    public string EventType { get; set; } = "UserCreated";
    public Guid UserId { get; set; }
}