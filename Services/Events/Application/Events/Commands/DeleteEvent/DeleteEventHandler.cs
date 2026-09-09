using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using MediatR;
using StackExchange.Redis;

namespace Events.Application.Events.Commands.DeleteEvent;

public class DeleteEventHandler(IEventRepository eventRepository, ICacheService cacheService) : IRequestHandler<DeleteEventCommand>
{
    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        await eventRepository.DeleteByIdAsync(request.Id);
        await cacheService.DeleteObjectJson($"event:{request.Id}");
    }
}