using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Presentation.Repositories;

public interface IEventRepository
{
    IQueryable<Event> GetWithFilter(string? title = null, DateTime? from = null, DateTime? to = null);
    IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize);
    Task<Event> GetByIdAsync(Guid id);
    Task<bool> ContainsByIdAsync(Guid id);

    Task<Guid> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats);
    Task UpdateAsync(Guid id, Event data);
    Task DeleteByIdAsync(Guid id);
}

public class EventRepository(ILogger<EventRepository> logger, AppDbContext db) : IEventRepository
{
    public async Task<Event> GetByIdAsync(Guid id)
    {
        var eventOne = await db.Events.Where(x => x.Id == id).FirstOrDefaultAsync();
        if (eventOne != null)
            return eventOne;

        logger.LogError($"Event with id {id} not found");
        throw new EventNotFoundException("Event not found");
    }

    public async Task<bool> ContainsByIdAsync(Guid id)
    {
        return await db.Events.AnyAsync(x => x.Id == id);
    }

    public async Task<Guid> CreateAsync(string title, string? description, DateTime startAt, DateTime endAt,
        int totalSeats)
    {
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

    public async Task UpdateAsync(Guid id, Event data)
    {
        var eventEntity = await db.Events.FindAsync(id);

        if (eventEntity != null)
        {
            eventEntity.Title = data.Title;
            eventEntity.Description = data.Description;
            eventEntity.StartAt = data.StartAt;
            eventEntity.EndAt = data.EndAt;

            await db.SaveChangesAsync();
            return;
        }

        logger.LogError($"Event with id {id} not found");
        throw new EventNotFoundException("Event not found");
    }

    public async Task DeleteByIdAsync(Guid id)
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

    public IQueryable<Event> Pagination(IQueryable<Event> events, int page, int pageSize)
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

    public IQueryable<Event> GetWithFilter(string? title = null, DateTime? from = null,
        DateTime? to = null)
    {
        // Сработает только в postgres
        if (db.Database.ProviderName?.Contains("Npgsql") == true)
            return db.Events.Where(x =>
                    (from == null || x.StartAt >= from) &&
                    (to == null || x.EndAt <= to) &&
                    (title == null || EF.Functions.ILike(x.Title, $"%{title}%")))
                .Select(x => x);

        // Для иных бд
        return db.Events.Where(x =>
                (from == null || x.StartAt >= from) &&
                (to == null || x.EndAt <= to) &&
                (title == null || x.Title.ToLower().Contains(title.ToLower())))
            .Select(x => x);
    }
}