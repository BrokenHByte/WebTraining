using MediatR;

namespace Application.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand : IRequest
{
    public required Guid ExistingId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}