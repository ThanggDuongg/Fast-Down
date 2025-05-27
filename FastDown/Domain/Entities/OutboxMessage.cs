using System.Text.Json;

namespace FastDown.Domain.Entities
{
    public class OutboxMessage
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public string EventData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        
        public static OutboxMessage Create<T>(T @event) where T : class
        {
            return new OutboxMessage
            {
                EventType = @event.GetType().Name,
                EventData = JsonSerializer.Serialize(@event),
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}