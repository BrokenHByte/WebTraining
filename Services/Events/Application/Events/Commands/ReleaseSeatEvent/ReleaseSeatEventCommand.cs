using MediatR;

namespace Events.Application.Events.Commands.ReleaseSeatEvent;

public class ReleaseSeatEventCommand : IRequest
{
    public required Guid EventId { get; set; }
}