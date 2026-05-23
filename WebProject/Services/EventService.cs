using System.Collections.Concurrent;
using WebProject.Exceptions;
using WebProject.Models;

namespace WebProject.Services;

public interface IEventService
{
    // READ
    IEnumerable<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null);
    IEnumerable<Event> GetPage(IEnumerable<Event> courses, int page, int pageSize);
    Event GetEventById(Guid id);
    bool ContainsById(Guid id);

    // WRITE
    Guid AddEvent(string title, string? description, DateTime startAt, DateTime endAt);
    void UpdateEvent(Guid id, Event data);
    void DeleteEventById(Guid id);
}

// Синглтоновский сервис
public class EventService(ILogger<EventService> logger) : IEventService
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();
    private int _counterId = 0;

    // При конкурентном доступе возможно большинство ошибок ниже, это штатная ситуация
    // но всё равно довольно редкая

    public IEnumerable<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null)
    {
        return _events.Where(x =>
                (from == null || x.Value.StartAt >= from) &&
                (to == null || x.Value.EndAt <= to) &&
                (title == null || x.Value.Title.Contains(title, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Value);
    }

    public IEnumerable<Event> GetPage(IEnumerable<Event> events, int page, int pageSize)
    {
        if (page <= 0)
        {
            logger.LogError($"Page {page} is invalid");
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (pageSize <= 0)
        {
            logger.LogError($"Page size {pageSize} is invalid");
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        return events
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    public Event GetEventById(Guid id)
    {
        if (!_events.TryGetValue(id, out var eventById))
        {
            logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }

        return eventById;
    }

    public void UpdateEvent(Guid id, Event data)
    {
        // Тут так же отказ, это возможно это штатная ситуация
        // но довольно редкая
        ValidateNewEvent(data.StartAt, data.EndAt);
        if (!_events.TryGetValue(id, out var existingEvent))
        {
            logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }

        var updatedEvent = new Event
        {
            Id = existingEvent.Id,
            Title = data.Title,
            Description = data.Description,
            StartAt = data.StartAt,
            EndAt = data.EndAt
        };

        if (!_events.TryUpdate(id, updatedEvent, existingEvent))
        {
            logger.LogError($"Event with id {data.Id} not found");
            throw new EventNotFoundException("Event not found");
        }
    }

    public void DeleteEventById(Guid id)
    {
        if (!_events.TryRemove(id, out _))
        {
            logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }
    }

    public bool ContainsById(Guid id)
    {
        return _events.ContainsKey(id);
    }

    public Guid AddEvent(string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidateNewEvent(startAt, endAt);
        var newId = Guid.NewGuid();
        var newEvent = new Event
        {
            Id = newId,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt
        };
        _events.TryAdd(newId, newEvent);
        return newId;
    }

    private void ValidateNewEvent(DateTime StartAt, DateTime EndAt)
    {
        if (EndAt <= StartAt)
        {
            logger.LogError("Event is invalid: EndAt <= StartAt");
            throw new EventValidationException("Event with id is invalid: EndAt <= StartAt");
        }
    }
}