using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Application.Events.Queries.GetEventById;
using Events.Application.Events.Queries.GetEventsPage;
using MediatR;
using StackExchange.Redis;

namespace Events.Application.Events.Queries.GetTopEvents;

public class GetTopEventsHandler(IEventRepository eventRepository, ICacheService cacheService) : IRequestHandler<GetTopEventsQuery, GetTopEventsResponse>
{
    public async Task<GetTopEventsResponse> Handle(GetTopEventsQuery request, CancellationToken cancellationToken)
    {
        var cacheValue = await cacheService.GetObjectJson<GetTopEventsResponse>("events:top10");
        if (cacheValue != null)
        {
            return cacheValue;
        }
        
        var top10 = await eventRepository.GetTop10();
        var events = top10.Select(o => new GetEventByIdResponse
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartAt = o.StartAt,
            EndAt = o.EndAt,
            TotalSeats = o.TotalSeats,
            AvailableSeats = o.AvailableSeats
        }).ToArray();

        var top10Events = new GetTopEventsResponse
        {
            Events = events
        };
        await cacheService.SetObjectJson("events:top10", top10Events);
        return top10Events;
    }
}