using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Repositories;

namespace WebProject.Services;

public interface IEventService
{
    // READ
    IQueryable<Event> GetWithFilter(string? title = null, DateTime? from = null, DateTime? to = null);
    IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize);
    Task<Event> GetByIdAsync(Guid id);
    Task<bool> ContainsByIdAsync(Guid id);

    // WRITE
    Task<Guid> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task UpdateAsync(Guid id, Event data);
    Task DeleteByIdAsync(Guid id);
}

// Синглтоновский сервис
public class EventService(IEventRepository eventRepository, ILogger<EventService> logger) : IEventService
{
    public Task<Event> GetByIdAsync(Guid id)
    {
        return eventRepository.GetByIdAsync(id);
    }

    public Task<bool> ContainsByIdAsync(Guid id)
    {
        return eventRepository.ContainsByIdAsync(id);
    }

    public Task<Guid> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt,
        int totalSeats)
    {
        ValidateDateEvent(startAt, endAt);
        return eventRepository.CreateAsync(title, description, startAt, endAt, totalSeats);
    }

    public Task UpdateAsync(Guid id, Event data)
    {
        ValidateDateEvent(data.StartAt, data.EndAt);
        return eventRepository.UpdateAsync(id, data);
    }

    public Task DeleteByIdAsync(Guid id)
    {
        return eventRepository.DeleteByIdAsync(id);
    }

    public IQueryable<Event> GetWithFilter(string? title = null, DateTime? from = null,
        DateTime? to = null)
    {
        return eventRepository.GetWithFilter(title, from, to);
    }

    public IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize)
    {
        return eventRepository.Pagination(events, page, pageSize);
    }

    private void ValidateDateEvent(DateTime startAt, DateTime endAt)
    {
        if (endAt <= startAt)
        {
            logger.LogError("Event is invalid: EndAt <= StartAt");
            throw new EventValidationException("Event with id is invalid: EndAt <= StartAt");
        }
    }
}