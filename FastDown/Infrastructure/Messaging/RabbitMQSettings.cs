namespace FastDown.Infrastructure.Messaging
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public int Port { get; set; } = 5672; // default port
        public string ExchangeName { get; set; } = "fastdown_events";
    }
}
