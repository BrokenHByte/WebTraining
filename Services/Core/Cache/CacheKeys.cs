namespace Contracts.Cache;

public record DataKey(string Key, int Ttl);

public class CacheKeys
{
    public static readonly DataKey KeyGetEventById = new("event:", 10);
    public static readonly DataKey KeyGetTopEvents = new("events:top10", 15);    
}