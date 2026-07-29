using Application.Abstractions.Persistence.Repositories;
using Application.Events.Common;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Events.Commands.CreateEvent;

public class CreateEventHandler(IEventRepository eventRepository, ILogger<CreateEventHandler> logger) : IRequestHandler<CreateEventCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        DateEventValidator.Check(request.StartAt, request.EndAt);
        return await eventRepository.CreateAsync(request.Title, request.Description, request.StartAt, request.EndAt, request.TotalSeats);
    }
}