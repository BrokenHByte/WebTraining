using MediatR;

namespace Application.Events.Commands.CreateEvent;

public sealed record CreateEventCommand : IRequest<Guid>
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public int TotalSeats { get; init; }
}