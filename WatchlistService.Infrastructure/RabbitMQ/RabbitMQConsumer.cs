using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using WatchlistService.Core.Interfaces;

namespace WatchlistService.Infrastructure.RabbitMQ;

public class RabbitMQConsumer : IAsyncDisposable
{
    private readonly IWatchlistService _watchlistService;
    private readonly string _hostname = "localhost";
    private readonly string _queueName = "WatchlistQueue";
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed = false;

    public RabbitMQConsumer(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    public async Task StartListeningAsync()
    {
        var factory = new ConnectionFactory { HostName = _hostname };
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
                    await _watchlistService.CreateWatchlistAsync(eventObj.UserId);
                }

                // Acknowledge message
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to inject ILogger)
                Console.WriteLine($"Error processing message: {ex.Message}");

                // Reject and requeue the message on error (optional)
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
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