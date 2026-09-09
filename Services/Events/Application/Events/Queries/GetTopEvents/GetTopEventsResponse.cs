using Events.Application.Events.Queries.GetEventById;

namespace Events.Application.Events.Queries.GetTopEvents;

public record GetTopEventsResponse
{ 
    public required ReadOnlyMemory<GetEventByIdResponse> Events { get; set; }
}