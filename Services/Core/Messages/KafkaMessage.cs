namespace Contracts.Messages;

public abstract class KafkaMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; }
    public string MessageType { get; set; }
}