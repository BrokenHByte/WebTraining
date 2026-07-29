using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using MediatR;

namespace Application.Events.Commands.CreateEvent;

public class CreateEventHandler(IEventService eventService) : IRequestHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        return await eventService.CreateAsync(request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);
    }
}