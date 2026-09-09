using System.Text.Json;
using Events.Application.Abstractions.Persistence.Repositories;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Events.Infrastructure.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Infrastructure.Data.Repositories;


public class EventRepository : IEventRepository
{
    private readonly IDatabase _cacheDb;
    private readonly ILogger<EventRepository> _logger;
    private readonly AppDbContext _db;
    private readonly int _defaultTTL;
    
    public EventRepository(ILogger<EventRepository> logger, AppDbContext db, IConnectionMultiplexer redis,
        IOptions<RedisConfig> config)
    {
        _cacheDb = redis.GetDatabase();
        _defaultTTL = config.Value.DefaultTTLMinutes;
    }
    
    
    public async Task<Event> GetByIdAsync(Guid id)
    {
        var redisValue = await _cacheDb.StringGetAsync("event:{id}");
        if (redisValue.HasValue)
        {
            Event? eventCache= JsonSerializer.Deserialize<Event>(redisValue.ToString());
            if(eventCache != null)
                return eventCache;
            _logger.LogDebug($"Event with ID {id} is missing from Redis.");
        }
        
        var eventOne = await _db.Events.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (eventOne != null)
        {
            var json = JsonSerializer.Serialize(eventOne);
            await _cacheDb.StringSetAsync("event:{id}", json, TimeSpan.FromMinutes(_defaultTTL));
            _logger.LogDebug($"Event with id {id} from redis");
            return eventOne; 
        }
      

        _logger.LogError($"Event with id {id} not found");
        throw new EventNotFoundException("Event not found");
    }

    public async Task<bool> ContainsByIdAsync(Guid id)
    {
        return await _db.Events.AnyAsync(x => x.Id == id);
    }

    public async Task<Guid> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt,
        int totalSeats)
    {
        var newId = Guid.NewGuid();
        await _db.Events.AddAsync(new Event
        {
            Id = newId,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        });

        await _db.SaveChangesAsync();
        return newId;
    }

    public async Task UpdateAsync(Guid id, Event data)
    {
        var eventEntity = await _db.Events.FindAsync(id);

        // Не меняет доступные места и оставшееся место. Спорный момент
        if (eventEntity != null)
        {
            eventEntity.Title = data.Title;
            eventEntity.Description = data.Description;
            eventEntity.StartAt = data.StartAt;
            eventEntity.EndAt = data.EndAt;

            await _db.SaveChangesAsync();
            var json = JsonSerializer.Serialize(eventEntity);
            await _cacheDb.StringSetAsync("event:{id}", json, TimeSpan.FromMinutes(_defaultTTL));
            _logger.LogDebug($"Event with id {id} cache update");
            return;
        }
        
        _logger.LogError($"Event with id {id} not found");
        throw new EventNotFoundException("Event not found");
    }

    public async Task DeleteByIdAsync(Guid id)
    {
        var oneEvent = await _db.Events.Where(x => x.Id == id).FirstOrDefaultAsync();

        if (oneEvent == null)
        {
            _logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }

        _db.Events.Remove(oneEvent);
        await _db.SaveChangesAsync();
        await _cacheDb.KeyDeleteAsync("event:{id}");
    }
    
    public IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize)
    {
        if (page <= 0)
        {
            _logger.LogError($"Page {page} is invalid");
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (pageSize <= 0)
        {
            _logger.LogError($"Page size {pageSize} is invalid");
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        return events
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    public IQueryable<Event> GetWithFilter(string? title = null, DateTime? from = null,
        DateTime? to = null)
    {
        // Сработает только в postgres
        if (_db.Database.ProviderName?.Contains("Npgsql") == true)
            return _db.Events.Where(x =>
                    (from == null || x.StartAt >= from) &&
                    (to == null || x.EndAt <= to) &&
                    (title == null || EF.Functions.ILike(x.Title, $"%{title}%")))
                .Select(x => x);

        // Для иных бд
        return _db.Events.Where(x =>
                (from == null || x.StartAt >= from) &&
                (to == null || x.EndAt <= to) &&
                (title == null || x.Title.ToLower().Contains(title.ToLower())))
            .Select(x => x);
    }
    
    public async Task<List<Event>> GetTop10()
    {
        var topList = await _db.Events.OrderBy(p => (p.TotalSeats - p.AvailableSeats) / p.TotalSeats).Take(10).ToListAsync();
        if(topList.Count == 0)
            return topList;
        var json = JsonSerializer.Serialize(topList);
        await _cacheDb.StringSetAsync("events:top10", json, TimeSpan.FromMinutes(_defaultTTL));
        return topList;
    }  

}