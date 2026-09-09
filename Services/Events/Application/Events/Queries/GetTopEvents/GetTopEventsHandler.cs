using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Events.Queries.GetEventById;
using Events.Application.Events.Queries.GetEventsPage;
using MediatR;

namespace Events.Application.Events.Queries.GetTopEvents;

public class GetTopEventsHandler(IEventRepository eventRepository) : IRequestHandler<GetTopEventsQuery, GetTopEventsResponse>
{
    public async Task<GetTopEventsResponse> Handle(GetTopEventsQuery request, CancellationToken cancellationToken)
    {
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
        return top10Events;
    }
}