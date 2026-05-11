using System.Collections.Concurrent;
using WebProject.Models;

namespace WebProject.Services;

public interface IEventService
{
    // READ
    IEnumerable<Event> GetEvents(string? title, DateTime? from, DateTime? to);
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

    public IEnumerable<Event> GetEvents(string? title, DateTime? from, DateTime? to)
    {
        return _events.Where(x =>
                (title == null || x.Value.Title == title) &&
                (from == null || x.Value.StartAt == from) &&
                (to == null || x.Value.EndAt == to)).Select(pair => pair.Value);
    }
    
    public IEnumerable<Event> GetPage(IEnumerable<Event> courses, int page, int pageSize)
    {
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
        var newId = Interlocked.Increment(ref _counterId) - 1;
        data.Id = newId;
        return _events.TryAdd(newId, data);
    }

    public bool UpdateEvent(int id, Event data)
    {
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
}

