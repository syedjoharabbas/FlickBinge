using RabbitMQ.Client;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace UserService.Infrastructure.RabbitMQ
{
    public class RabbitMQPublisher : IAsyncDisposable
    {
        private readonly string _hostname = "localhost";
        private readonly int _port = 5672;
        private readonly string _queueName = "WatchlistQueue";
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _initialized = false;
        private readonly ILogger<RabbitMQPublisher> _logger;

        public RabbitMQPublisher(ILogger<RabbitMQPublisher> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;

            var factory = new ConnectionFactory { HostName = _hostname, Port=_port};
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _initialized = true;
        }

        public async Task PublishUserCreatedAsync(Guid userId)
        {
            if (!_initialized)
                await InitializeAsync();

            var userEvent = new
            {
                EventType = "UserCreated",
                UserId = userId
            };

            var message = JsonSerializer.Serialize(userEvent);
            var body = Encoding.UTF8.GetBytes(message);

            // Publish directly to the queue using default exchange
            var channel = _channel ?? throw new InvalidOperationException("RabbitMQ channel not initialized. Call InitializeAsync first.");
            await channel.BasicPublishAsync(
                exchange: "",            // default exchange
                routingKey: _queueName,  // queue name
                body: body
            );

            _logger.LogInformation("[UserService] Sent UserCreated for {UserId}", userId);
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
            if (_connection is not null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
