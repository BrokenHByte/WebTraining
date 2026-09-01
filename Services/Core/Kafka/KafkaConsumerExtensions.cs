using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Contracts.Kafka;

public static class KafkaConsumerExtensions
{
    public static IServiceCollection AddKafkaConsumer<TMessage, TCommand>(
        this IServiceCollection services,
        IConfiguration configuration,
        string topic,
        string groupId,
        Func<TMessage, TCommand> commandFactory,
        Action<ConsumerConfig>? configure = null)
        where TMessage : class
        where TCommand : IRequest
    {
        services.AddHostedService(sp =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            configure?.Invoke(config);

            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<ILogger<KafkaConsumer<TMessage, TCommand>>>();

            return new KafkaConsumer<TMessage, TCommand>(
                config,
                topic,
                commandFactory,
                scopeFactory,
                logger
            );
        });

        return services;
    }
}