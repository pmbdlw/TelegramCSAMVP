namespace CSAFacade;

public class RabbitMQConfig
{
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string ExchangeName { get; set; } = "telegram_events";
    public string QueueName { get; set; } = "telegram_messages";
    public string RoutingKey { get; set; } = "message";
}