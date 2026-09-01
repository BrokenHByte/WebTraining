using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Contracts.Messages;
using Microsoft.Extensions.Logging;

namespace Contracts.Kafka;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _bootstrapServers;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<KafkaProducerService> _logger;
    
    public KafkaProducerService(
        string bootstrapServers, string clientId, 
        ILogger<KafkaProducerService> logger)
    {
        _bootstrapServers = bootstrapServers;
        _logger = logger;
        
        var config = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            ClientId = clientId,
            Acks = Acks.All,
            MessageTimeoutMs = 5000,
            EnableDeliveryReports = true
        };
        
        _producer = new ProducerBuilder<string, string>(config).Build();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    public async Task SendAsync<T>(string topic, Guid key, T message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string> 
                { 
                    Key = key.ToString(),
                    Value = json,
                    Headers = new Headers
                    {
                        { "message-type", Encoding.UTF8.GetBytes(typeof(T).Name) },
                        { "timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.Ticks.ToString()) }
                    }
                }
            );
            
            _logger.LogInformation(
                "Сообщение {MessageType} отправлено в топик {Topic}, Partition: {Partition}, Offset: {Offset}",
                typeof(T).Name,
                topic,
                result.Partition,
                result.Offset
            );
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщения в топик {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}