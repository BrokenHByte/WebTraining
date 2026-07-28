using Domain.Entities;

namespace Application.Abstractions.Persistence.Repositories;

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