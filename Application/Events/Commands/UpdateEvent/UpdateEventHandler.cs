using Application.Abstractions.Persistence.Services;
using Domain.Entities;
using MediatR;

namespace Application.Events.Commands.UpdateEvent;

public class UpdateEventHandler(IEventService eventService) : IRequestHandler<UpdateEventCommand>
{
    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        await eventService.UpdateAsync(request.ExistingId, new Event()
        {
            Id = request.ExistingId,
            Title =  request.Title,
            Description = request.Description,
            StartAt =  request.StartAt,
            EndAt =   request.EndAt
        });
    }
}