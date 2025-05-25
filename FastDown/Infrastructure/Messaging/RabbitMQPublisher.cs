using System.Text;
using System.Text.Json;
using FastDown.Core;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FastDown.Infrastructure.Messaging
{
    public class RabbitMQPublisher : IEventPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly RabbitMQSettings _settings;
        private bool _disposed;

        public RabbitMQPublisher(IOptions<RabbitMQSettings> settings)
        {
            _settings = settings.Value;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                UserName = _settings.UserName,
                Password = _settings.Password,
                Port = _settings.Port,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false
            );
        }

        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : class
        {
            var eventName = @event.GetType().Name;
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: eventName,
                basicProperties: null,
                body: body
            );

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Dispose managed resources
                _channel?.Close();
                _channel?.Dispose();

                _connection?.Close();
                _connection?.Dispose();
            }

            _disposed = true;
        }
    }
}
