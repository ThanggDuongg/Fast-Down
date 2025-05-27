using System.Text;
using System.Text.Json;
using FastDown.Domain.Events;
using FastDown.Domain.ReadModels;
using FastDown.Infrastructure.Messaging;
using FastDown.Infrastructure.Persistence.MongoDB;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FastDown.Worker
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly string _queueName;

        public RabbitMQConsumerService(
            IOptions<RabbitMQSettings> settings,
            IServiceProvider serviceProvider,
            ILogger<RabbitMQConsumerService> logger
        )
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var rabbitSettings = settings.Value;

            var factory = new ConnectionFactory
            {
                HostName = rabbitSettings.HostName,
                UserName = rabbitSettings.UserName,
                Password = rabbitSettings.Password,
                Port = rabbitSettings.Port,
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: rabbitSettings.ExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false
            );

            _queueName = _channel.QueueDeclare().QueueName;

            _channel.QueueBind(
                queue: _queueName,
                exchange: rabbitSettings.ExchangeName,
                routingKey: ""
            );
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = eventArgs.RoutingKey;

                try
                {
                    await ProcessMessage(routingKey, message);
                    _channel.BasicAck(eventArgs.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {Message}", message);
                    _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessMessage(string routingKey, string message)
        {
            _logger.LogInformation(
                "Processing message: {RoutingKey}, {Message}",
                routingKey,
                message
            );

            // Create a scope to resolve scoped services
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<DownloadTaskReadRepository>();

            try
            {
                if (routingKey == nameof(DownloadTaskCreatedEvent))
                {
                    var @event = JsonSerializer.Deserialize<DownloadTaskCreatedEvent>(message);
                    if (@event != null)
                    {
                        await ProcessDownloadTaskCreatedEvent(@event, repository);
                        _logger.LogInformation(
                            "Successfully processed DownloadTaskCreatedEvent for ID: {Id}",
                            @event.Id
                        );
                    }
                }
                else if (routingKey == nameof(DownloadTaskStatusChangedEvent))
                {
                    var @event = JsonSerializer.Deserialize<DownloadTaskStatusChangedEvent>(
                        message
                    );
                    if (@event != null)
                    {
                        await ProcessDownloadTaskStatusChangedEvent(@event, repository);
                        _logger.LogInformation(
                            "Successfully processed DownloadTaskStatusChangedEvent for ID: {Id}",
                            @event.Id
                        );
                    }
                }
                else if (routingKey == nameof(DownloadTaskProgressEvent))
                {
                    var @event = JsonSerializer.Deserialize<DownloadTaskProgressEvent>(message);
                    if (@event != null)
                    {
                        await ProcessDownloadTaskProgressEvent(@event, repository);
                        _logger.LogInformation(
                            "Successfully processed DownloadTaskProgressEvent for ID: {Id}",
                            @event.Id
                        );
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing message with routing key {RoutingKey}",
                    routingKey
                );
            }
        }

        private async Task ProcessDownloadTaskCreatedEvent(
            DownloadTaskCreatedEvent @event,
            DownloadTaskReadRepository repository
        )
        {
            var readModel = new DownloadTaskReadModel
            {
                OriginalId = @event.Id,
                Url = @event.Url,
                FileName = @event.FileName,
                Status = @event.Status,
                CreatedAt = @event.CreatedAt,
            };

            await repository.UpsertAsync(readModel);
            _logger.LogInformation("Created read model for download task: {Id}", @event.Id);
        }

        private async Task ProcessDownloadTaskStatusChangedEvent(
            DownloadTaskStatusChangedEvent @event,
            DownloadTaskReadRepository repository
        )
        {
            var readModel = await repository.GetByIdAsync(@event.Id);
            if (readModel != null)
            {
                readModel.Status = @event.Status;
                await repository.UpsertAsync(readModel);
                _logger.LogInformation(
                    "Updated status for download task: {Id} to {Status}",
                    @event.Id,
                    @event.Status
                );
            }
        }

        private async Task ProcessDownloadTaskProgressEvent(
            DownloadTaskProgressEvent @event,
            DownloadTaskReadRepository repository
        )
        {
            var readModel = await repository.GetByIdAsync(@event.Id);
            if (readModel != null)
            {
                // Update progress information
                readModel.BytesDownloaded = @event.BytesDownloaded;
                readModel.TotalBytes = @event.TotalBytes;
                readModel.ProgressPercentage = @event.ProgressPercentage;
                readModel.LastProgressUpdate = @event.Timestamp;

                await repository.UpsertAsync(readModel);

                _logger.LogInformation(
                    "Updated progress for download task: {Id} to {Progress}%",
                    @event.Id,
                    @event.ProgressPercentage
                );
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
