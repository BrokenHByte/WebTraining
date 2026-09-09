namespace Events.Application.Abstractions.Persistence.Services;

public interface ICacheService
{
    public Task SetObjectJson<T>(string key, T objectJson, int? ttl = null);
    public Task<T?> GetObjectJson<T>(string key);
    public Task DeleteObjectJson(string key);
}