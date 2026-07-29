using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Application.Events.Commands.UpdateEvent;
using MediatR;

namespace Application.Events.Commands.DeleteEvent;

public class DeleteEventHandler(IEventService eventService) : IRequestHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        await eventService.DeleteByIdAsync(request.Id);
    }
}