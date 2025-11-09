namespace Ats.Messaging.Options;

public sealed class MessagingOptions
{
    // Подключение к брокеру
    public string HostName { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "ats_user";
    public string Password { get; set; } = "ats_pass";
    public string VirtualHost { get; set; } = "/";
    public string ClientId { get; set; } = "default-client";

    // Exchange по умолчанию (topic)
    public string Exchange { get; set; } = "ats.events";

    // Поведение
    public bool PublisherConfirms { get; set; } = false; // можно включить при необходимости
    public int PrefetchCount { get; set; } = 16;         // QoS для consumer
}