using System.Collections.Concurrent;
using WebTraining.Models;

namespace WebTraining.Services;

public interface IEventService
{
    // READ
    IEnumerable<Event> GetEvents();     
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

    public IEnumerable<Event> GetEvents()
    {
        // Безопасно для параллельного удаления/вставки
        return _events.Select(pair => pair.Value);
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

