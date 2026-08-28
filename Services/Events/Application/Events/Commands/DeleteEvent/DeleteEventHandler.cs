using Application.Abstractions.Persistence.Repositories;
using MediatR;

namespace Application.Events.Commands.DeleteEvent;

public class DeleteEventHandler(IEventRepository eventRepository) : IRequestHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        await eventRepository.DeleteByIdAsync(request.Id);
    }
}