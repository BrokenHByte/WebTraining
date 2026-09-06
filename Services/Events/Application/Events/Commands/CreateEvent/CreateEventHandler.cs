using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Events.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Events.Application.Events.Commands.CreateEvent;

public class CreateEventHandler(IEventRepository eventRepository, ILogger<CreateEventHandler> logger) : IRequestHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        DateEventValidator.Check(request.StartAt, request.EndAt);
        return await eventRepository.CreateAsync(request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);
    }
}