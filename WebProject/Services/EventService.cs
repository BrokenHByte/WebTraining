using System.Collections.Concurrent;
using WebProject.Models;

namespace WebProject.Services;

public interface IEventService
{
    // READ
    IEnumerable<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null);
    IEnumerable<Event> GetPage(IEnumerable<Event> courses, int page, int pageSize);
    Event? GetEventById(int id); 
    
    // WRITE
    bool AddEvent(Event data);
    bool UpdateEvent(int id, Event data);    
    bool DeleteEventById(int id); 
}

// Синглтоновский сервис
public class EventService : IEventService
{
    private ConcurrentDictionary<int, Event> _events = new();
    private int _counterId = 0;

    public IEnumerable<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _events.AsQueryable();
        // Разделение ради минимизации проверок с StartsWith.
        if (from != null) query = query.Where(x => x.Value.StartAt >= from);
        if (to != null) query = query.Where(x => x.Value.EndAt <= to);
        if (title != null) query = query.Where(x => x.Value.Title.StartsWith(title, StringComparison.OrdinalIgnoreCase));
        return query.Select(pair => pair.Value);
    }
    
    public IEnumerable<Event> GetPage(IEnumerable<Event> courses, int page, int pageSize)
    {
        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        return courses
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    } 
    
    public Event? GetEventById(int id)
    {
        _events.TryGetValue(id, out Event? data);
        return data;
    }

    public bool AddEvent(Event data)
    {
        if (!ValidateNewEvent(data))
            return false;
        var newId = Interlocked.Increment(ref _counterId) - 1;
        data.Id = newId;
        return _events.TryAdd(newId, data);
    }

    public bool UpdateEvent(int id, Event data)
    {
        if (!ValidateNewEvent(data))
            return false;
        if (!_events.TryGetValue(id, out var existingEvent))
            return false;

        // Делаем "иммутабельно"
        var updatedEvent = new Event
        {
            Id = existingEvent.Id,
            Title = data.Title,
            Description = data.Description,
            StartAt = data.StartAt,
            EndAt = data.EndAt
        };

        return _events.TryUpdate(id, updatedEvent, existingEvent);
    }

    public bool DeleteEventById(int id)
    {
        return _events.TryRemove(id, out _);
    }
    
    bool ValidateNewEvent(Event data)
    {
        if (data.EndAt <= data.StartAt)
            return false;
        return true;
    }
    
}

