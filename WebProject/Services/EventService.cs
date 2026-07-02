using WebProject.Models;
using WebProject.Repositories;

namespace WebProject.Services;

public interface IEventService
{
    // READ
    IQueryable<Event> GetEvents(string? title = null, DateTime? from = null, DateTime? to = null);
    IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize);
    Task<Event> GetEventByIdAsync(Guid id);
    Task<bool> ContainsByIdAsync(Guid id);

    // WRITE
    Task<Guid> AddEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task UpdateEventAsync(Guid id, Event data);
    Task DeleteEventByIdAsync(Guid id);
}

// Синглтоновский сервис
public class EventService(ILogger<EventService> logger, IEventRepository eventRepository) : IEventService
{
    public Task<Event> GetEventByIdAsync(Guid id)
    {
        return eventRepository.GetEventByIdAsync(id);
    }

    public Task<bool> ContainsByIdAsync(Guid id)
    {
        return eventRepository.ContainsByIdAsync(id);
    }

    public Task<Guid> AddEventAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)
    {
        return eventRepository.AddEventAsync(title, description, startAt, endAt, totalSeats);
    }

    public Task UpdateEventAsync(Guid id, Event data)
    {
        return eventRepository.UpdateEventAsync(id, data);
    }

    public Task DeleteEventByIdAsync(Guid id)
    {
        return eventRepository.DeleteEventByIdAsync(id);
    }

    public IQueryable<Event> GetEvents(string? title = null, DateTime? from = null,
        DateTime? to = null)
    {
        return eventRepository.GetEvents(title, from, to);
    }

    public IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize)
    {
        return eventRepository.Pagination(events, page, pageSize);
    }
}