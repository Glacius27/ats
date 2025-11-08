using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Ats.Messaging.Abstractions;
using Ats.Messaging.Internal;
using Ats.Messaging.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ats.Messaging;

public sealed class RabbitMqSubscriber : IEventSubscriber
{
    private readonly RabbitMqConnection _connProvider;
    private readonly MessagingOptions _opt;
    private readonly ILogger<RabbitMqSubscriber>? _logger;

    private IConnection? _conn;
    private IChannel? _channel;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqSubscriber(RabbitMqConnection connProvider, IOptions<MessagingOptions> opt, ILogger<RabbitMqSubscriber>? logger = null)
    {
        _connProvider = connProvider;
        _opt = opt.Value;
        _logger = logger;
    }

    private async Task EnsureAsync()
    {
        _conn ??= await _connProvider.GetOrCreateConnectionAsync();
        _channel ??= await _conn.CreateChannelAsync();

        // Prefetch (QoS)
        await _channel.BasicQosAsync(0, (ushort)_opt.PrefetchCount, global: false);
        // Объявим exchange по умолчанию (идемпотентно)
        await _channel.ExchangeDeclareAsync(_opt.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
    }

    public void Subscribe<TEvent>(string queueName, Func<TEvent, Task> handler, string? routingKey = null, string? exchange = null)
    {
        // fire-and-forget запуск подписки
        _ = SubscribeInternal(queueName, handler, routingKey, exchange);
    }

    private async Task SubscribeInternal<TEvent>(string queueName, Func<TEvent, Task> handler, string? routingKey, string? exchange)
    {
        await EnsureAsync();

        var ex = exchange ?? _opt.Exchange;
        var rk = routingKey ?? typeof(TEvent).Name;

        // Объявляем очередь (durable) и биндим
        await _channel!.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        await _channel.QueueBindAsync(queue: queueName, exchange: ex, routingKey: rk);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize<TEvent>(json, JsonOpts);

                if (obj is null)
                {
                    _logger?.LogWarning("Deserialization returned null for message on {Queue}", queueName);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await handler(obj);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling message on {Queue}", queueName);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        _logger?.LogInformation("Subscribed: queue={Queue} ex={Exchange} rk={RoutingKey} type={Type}",
            queueName, ex, rk, typeof(TEvent).Name);
    }
    
    public async Task RegisterSubscribersAsync(IServiceProvider serviceProvider)
{
    await EnsureAsync(); // убедимся, что соединение и канал есть

    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    var subscriberTypes = assemblies
        .SelectMany(a => a.GetTypes())
        .Where(t => typeof(IEventSubscriber).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

    foreach (var subscriberType in subscriberTypes)
    {
        var instance = (IEventSubscriber)ActivatorUtilities.CreateInstance(serviceProvider, subscriberType);

        var methods = subscriberType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttributes(typeof(TopicAttribute), false).Any());

        foreach (var method in methods)
        {
            var topicAttr = (TopicAttribute)method.GetCustomAttributes(typeof(TopicAttribute), false).First();
            var parameterType = method.GetParameters().FirstOrDefault()?.ParameterType;
            if (parameterType == null)
            {
                _logger?.LogWarning("Skipping {Method}: no parameter type found.", method.Name);
                continue;
            }

            var routingKey = topicAttr.Name;
            var queueName = $"{_opt.Exchange}.{routingKey}.{subscriberType.Name}".ToLower();

            _logger?.LogInformation("Auto-subscribing {Subscriber}.{Method} to {RoutingKey}", subscriberType.Name, method.Name, routingKey);

            // создаем типизированный handler динамически
            var subscribeMethod = typeof(RabbitMqSubscriber)
                .GetMethod(nameof(Subscribe), BindingFlags.Public | BindingFlags.Instance)!
                .MakeGenericMethod(parameterType);

            // создаём делегат для вызова метода подписчика
            var handlerDelegate = CreateHandlerDelegate(instance, method, parameterType);

            // вызываем Subscribe<T>(queueName, handler, routingKey)
            subscribeMethod.Invoke(this, new object?[] { queueName, handlerDelegate, routingKey, _opt.Exchange });
        }
    }
}

private static object CreateHandlerDelegate(object instance, MethodInfo method, Type parameterType)
{
    // Формируем Func<T, Task> с рефлексией
    var handlerType = typeof(Func<,>).MakeGenericType(parameterType, typeof(Task));

    return Delegate.CreateDelegate(handlerType, instance, method);
}

    public async ValueTask DisposeAsync()
    {
        try { if (_channel is not null) await _channel.CloseAsync(); } catch { }
        try { if (_conn is not null) await _conn.CloseAsync(); } catch { }
    }
}
