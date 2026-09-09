using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Application.Events.Commands.DeleteEvent;
using MediatR;

namespace Events.Application.Events.Commands.ReleaseSeatEvent;

public class ReleaseSeatEventHandler(IEventRepository eventRepository, ICacheService cacheService) : IRequestHandler<ReleaseSeatEventCommand>
{
    public async Task Handle(ReleaseSeatEventCommand request, CancellationToken cancellationToken)
    {
        var oneEvent = await eventRepository.GetByIdAsync(request.EventId);
        oneEvent.ReleaseSeats();
        await eventRepository.UpdateAsync(oneEvent.Id, oneEvent);
        await cacheService.DeleteObjectJson($"event:{oneEvent.Id}");
    }
}