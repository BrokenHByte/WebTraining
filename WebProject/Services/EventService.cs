using System.Collections.Concurrent;
using WebProject.Exceptions;
using WebProject.Models;

namespace WebProject.Services;

public interface IEventService
{
    // READ
    Task<IEnumerable<Event>> GetEventsAsync(string? title = null, DateTime? from = null, DateTime? to = null);
    Task<IEnumerable<Event>> GetPageAsync(IEnumerable<Event> courses, int page, int pageSize);
    Task<Event> GetEventByIdAsync(Guid id);
    Task<bool> ContainsByIdAsync(Guid id);

    // WRITE
    Task<Guid> AddEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int TotalSeats);
    void UpdateEventAsync(Guid id, Event data);
    void DeleteEventByIdAsync(Guid id);
}

// Синглтоновский сервис
public class EventService(ILogger<EventService> logger) : IEventService
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();
    // При конкурентном доступе возможно большинство ошибок ниже, это штатная ситуация
    // но всё равно довольно редкая

    public async Task<IEnumerable<Event>> GetEventsAsync(string? title = null, DateTime? from = null,
        DateTime? to = null)
    {
        // TODO: async для БД
        return _events.Where(x =>
                (from == null || x.Value.StartAt >= from) &&
                (to == null || x.Value.EndAt <= to) &&
                (title == null || x.Value.Title.Contains(title, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Value);
    }

    public async Task<IEnumerable<Event>> GetPageAsync(IEnumerable<Event> events, int page, int pageSize)
    {
        // TODO: async для БД
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

    public async Task<Event> GetEventByIdAsync(Guid id)
    {
        // TODO: async для БД
        if (!_events.TryGetValue(id, out var eventById))
        {
            logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }

        return eventById;
    }

    public async void DeleteEventByIdAsync(Guid id)
    {
        // TODO: async для БД
        if (!_events.TryRemove(id, out _))
        {
            logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }
    }

    public async Task<Guid> AddEventAsync(string title, string? description, DateTime startAt, DateTime endAt,
        int TotalSeats)
    {
        // TODO: async для БД
        ValidateDateEvent(startAt, endAt);
        var newId = Guid.NewGuid();
        var newEvent = new Event
        {
            Id = newId,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = TotalSeats,
            AvailableSeats = TotalSeats
        };

        _events.TryAdd(newId, newEvent);
        return newId;
    }

    public async void UpdateEventAsync(Guid id, Event data)
    {
        // TODO: async для БД
        ValidateDateEvent(data.StartAt, data.EndAt);

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
            EndAt = data.EndAt,
            TotalSeats = existingEvent.TotalSeats // Обновление количества мест недопустимо для такой модели
        };

        if (!_events.TryUpdate(id, updatedEvent, existingEvent))
        {
            logger.LogError($"Event with id {data.Id} not found");
            throw new EventNotFoundException("Event not found");
        }
    }

    public async Task<bool> ContainsByIdAsync(Guid id)
    {
        // TODO: async для БД
        return _events.ContainsKey(id);
    }

    private void ValidateDateEvent(DateTime StartAt, DateTime EndAt)
    {
        if (EndAt <= StartAt)
        {
            logger.LogError("Event is invalid: EndAt <= StartAt");
            throw new EventValidationException("Event with id is invalid: EndAt <= StartAt");
        }
    }
}