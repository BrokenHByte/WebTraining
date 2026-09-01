using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Contracts.Kafka;

public class KafkaConsumer<TMessage, TCommand> : BackgroundService
    where TMessage : class
    where TCommand : IRequest
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumer<TMessage, TCommand>> _logger;
    private readonly string _topic;
    private readonly Func<TMessage, TCommand> _commandFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaConsumer(
        ConsumerConfig config,
        string topic,
        Func<TMessage, TCommand> commandFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumer<TMessage, TCommand>> logger)
    {
        _topic = topic;
        _commandFactory = commandFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(topic);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);

                    if (result.IsPartitionEOF)
                        continue;

                    var message = JsonSerializer.Deserialize<TMessage>(result.Message.Value, _jsonOptions);

                    if (message != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                        // Создаем команду через фабрику
                        var command = _commandFactory(message);
                        await mediator.Send(command, stoppingToken);

                        _consumer.Commit(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from {Topic}", _topic);
                }
            }
        }, stoppingToken);
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}