namespace Ats.Messaging.Options;

public sealed class MessagingOptions
{
    // Подключение к брокеру
    public string HostName { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    // Exchange по умолчанию (topic)
    public string Exchange { get; set; } = "ats.events";

    // Поведение
    public bool PublisherConfirms { get; set; } = false; // можно включить при необходимости
    public int PrefetchCount { get; set; } = 16;         // QoS для consumer
}