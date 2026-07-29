using Application.Abstractions.Persistence.Repositories;
using Application.Events.Common;
using Domain.Entities;
using MediatR;

namespace Application.Events.Commands.UpdateEvent;

public class UpdateEventHandler(IEventRepository eventRepository) : IRequestHandler<UpdateEventCommand>
{
    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        DateEventValidator.Check(request.StartAt, request.EndAt);
        await eventRepository.UpdateAsync(request.ExistingId, new Event()
        {
            Id = request.ExistingId,
            Title =  request.Title,
            Description = request.Description,
            StartAt =  request.StartAt,
            EndAt =   request.EndAt
        });
    }
}