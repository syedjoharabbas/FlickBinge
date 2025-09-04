using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchlistService.Infrastructure.RabbitMQ
{
    public class RabbitMQBackgroundService : BackgroundService
    {
        private readonly RabbitMQConsumer _consumer;

        public RabbitMQBackgroundService(RabbitMQConsumer consumer)
        {
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _consumer.StartListeningAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _consumer.DisposeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
