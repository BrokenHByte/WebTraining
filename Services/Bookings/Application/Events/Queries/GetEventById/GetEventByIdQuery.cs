using MediatR;

namespace Application.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery : IRequest<GetEventByIdResponse>
{
    public required Guid Id { get; set; }
}