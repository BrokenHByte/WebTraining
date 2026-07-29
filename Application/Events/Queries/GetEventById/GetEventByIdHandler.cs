using Application.Abstractions.Persistence.Services;
using MediatR;

namespace Application.Events.Queries.GetEventById;

public class GetEventByIdHandler(IEventService eventService) : IRequestHandler<GetEventByIdQuery, GetEventByIdResponse>
{
    public async Task<GetEventByIdResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await eventService.GetByIdAsync(request.Id);
        return new GetEventByIdResponse()
        {
            Id = result.Id,
            Title = result.Title,
            Description = result.Description,
            StartAt =  result.StartAt,
            EndAt =  result.EndAt,
            AvailableSeats =  result.AvailableSeats,
            TotalSeats = result.TotalSeats
        };
    }
}