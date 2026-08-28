using MediatR;

namespace Events.Application.Events.Commands.DeleteEvent;

public sealed record DeleteEventCommand : IRequest
{
    public required Guid Id { get; set; }
}