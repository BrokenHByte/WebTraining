using Events.Application.Events.Queries.GetEventsPage;
using MediatR;

namespace Events.Application.Events.Queries.GetTopEvents;

public sealed record GetTopEventsQuery : IRequest<GetTopEventsResponse> { }