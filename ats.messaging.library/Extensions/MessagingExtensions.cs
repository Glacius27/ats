using Ats.Messaging;
using Ats.Messaging.Abstractions;
using Ats.Messaging.Internal;
using Ats.Messaging.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Ats.Messaging.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
        services.AddSingleton<RabbitMqSubscriber>();
        services.AddSingleton<IEventSubscriber, RabbitMqSubscriber>(); 

        return services;
    }

    public static async Task<IApplicationBuilder> UseMessagingAsync(this IApplicationBuilder app)
    {
        var subscriber = app.ApplicationServices.GetRequiredService<RabbitMqSubscriber>();
        await subscriber.RegisterSubscribersAsync(app.ApplicationServices);
        return app;
    }
}