using System.Text.Json;
using FastDown.Core;
using FastDown.Domain.Entities;
using FastDown.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FastDown.Infrastructure.Messaging
{
    public class OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger)
        : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<OutboxPublisher> _logger = logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing outbox messages");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FDContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var messages = await context
                .OutboxMessages.Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    await PublishMessageAsync(message, publisher, stoppingToken);

                    message.ProcessedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                }
            }
        }

        private async Task PublishMessageAsync(
            OutboxMessage message,
            IEventPublisher publisher,
            CancellationToken stoppingToken
        )
        {
            try
            {
                Type eventType =
                    AppDomain
                        .CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t =>
                            t.FullName == $"FastDown.Domain.Events.{message.EventType}"
                        )
                    ?? throw new InvalidOperationException(
                        $"Event type {message.EventType} not found"
                    );

                var @event = JsonSerializer.Deserialize(message.EventData, eventType);

                if (@event != null)
                {
                    await publisher.PublishAsync(@event, stoppingToken);
                    _logger.LogInformation(
                        "Published outbox message {Id} of type {EventType}",
                        message.Id,
                        message.EventType
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing message {Id} of type {EventType}",
                    message.Id,
                    message.EventType
                );
                throw;
            }
        }
    }
}
