using Application.Abstractions.Persistence.Repositories;
using Application.Events.Commands.UpdateEvent;
using Domain.Entities;
using MediatR;

namespace Application.Events.Queries.GetEventById;

public class GetEventByIdHandler(IEventRepository eventRepository) : IRequestHandler<GetEventByIdQuery, GetEventByIdResponse>
{
    public async Task<GetEventByIdResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await eventRepository.GetByIdAsync(request.Id);
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