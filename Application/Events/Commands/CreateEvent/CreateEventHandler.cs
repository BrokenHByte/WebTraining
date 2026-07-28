using Application.Abstractions.Persistence.Repositories;
using MediatR;

namespace Application.Events.Commands.CreateEvent;

public class CreateEventHandler(IEventRepository eventRepository) : IRequestHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        return await eventRepository.CreateAsync(request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);
    }
}