using Application.Abstractions.Persistence.Repositories;
using Application.Events.Queries.GetEventById;
using MediatR;

namespace Application.Events.Queries.GetEventsPage;

public class GetEventPageHandler(IEventRepository eventRepository) : IRequestHandler<GetEventPageQuery, GetEventPageResponse>
{    
    private readonly int _defaultPage = 1;
    private readonly int _defaultSizePage = 10;

    public async Task<GetEventPageResponse> Handle(GetEventPageQuery request, CancellationToken cancellationToken)
    {
        var query = eventRepository.GetWithFilter(request.Title, request.From, request.To);
        int countEvents = query.Count();
        int pageNumber = request.Page ?? _defaultPage;
        int validPageSize = request.PageSize ?? _defaultSizePage;

        var page = eventRepository.Pagination(query, pageNumber, validPageSize).ToList();
        var events = page.Select(o => new GetEventByIdResponse
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartAt = o.StartAt,
            EndAt = o.EndAt,
            TotalSeats = o.TotalSeats,
            AvailableSeats = o.AvailableSeats
        }).ToArray();
        
        var eventsPaginated = new GetEventPageResponse
        {
            TotalCountEvents = countEvents,
            CurrentPage = pageNumber,
            PageSize = page.Count,
            Events = events
        };
        return eventsPaginated;
    }
}