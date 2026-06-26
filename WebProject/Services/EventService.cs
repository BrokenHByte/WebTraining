using Microsoft.EntityFrameworkCore;
using WebProject.DataAccess;
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
    Task UpdateEventAsync(Guid id, Event data);
    Task DeleteEventByIdAsync(Guid id);
}

// Синглтоновский сервис
public class EventService(ILogger<EventService> logger, AppDbContext db) : IEventService
{
    public async Task<IEnumerable<Event>> GetEventsAsync(string? title = null, DateTime? from = null,
        DateTime? to = null)
    {
        return await db.Events.Where(x =>
                (from == null || x.StartAt >= from) &&
                (to == null || x.EndAt <= to) &&
                (title == null || x.Title.Contains(title, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x).ToListAsync();
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
        var eventOne = await db.Events.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (eventOne != null)
            return eventOne;
        logger.LogError($"Event with id {id} not found");
        throw new EventNotFoundException("Event not found");
    }

    public async Task<Guid> AddEventAsync(string title, string? description, DateTime startAt, DateTime endAt,
        int totalSeats)
    {
        ValidateDateEvent(startAt, endAt);
        var newId = Guid.NewGuid();
        await db.Events.AddAsync(new Event
        {
            Id = newId,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        });
        await db.SaveChangesAsync();
        return newId;
    }

    public async Task<bool> ContainsByIdAsync(Guid id)
    {
        return await db.Events.AnyAsync(x => x.Id == id);
    }

    public async Task DeleteEventByIdAsync(Guid id)
    {
        var oneEvent = await db.Events.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (oneEvent == null)
        {
            logger.LogError($"Event with id {id} not found");
            throw new EventNotFoundException("Event not found");
        }

        db.Events.Remove(oneEvent);
        await db.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(Guid id, Event data)
    {
        ValidateDateEvent(data.StartAt, data.EndAt);
        var eventEntity = await db.Events.FindAsync(id);
        if (eventEntity != null)
        {
            eventEntity.Title = data.Title;
            eventEntity.Description = data.Description;
            eventEntity.StartAt = data.StartAt;
            eventEntity.EndAt = data.EndAt;
            try
            {
                await db.SaveChangesAsync();
            }
            catch
            {
                throw new InvalidOperationException();
            }
            return;
        }

        logger.LogError($"Event with id {id} not found");
        throw new EventNotFoundException("Event not found");
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