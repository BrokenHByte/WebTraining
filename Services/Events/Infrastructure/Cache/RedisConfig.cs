namespace Events.Infrastructure.Cache;

public class RedisConfig
{
    public string ConnectionString { get; set; } = "";
    public int DefaultTTLMinutes { get; set; } = 10;
}