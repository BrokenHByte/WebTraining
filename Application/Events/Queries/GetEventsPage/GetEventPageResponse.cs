using Application.Events.Queries.GetEventById;

namespace Application.Events.Queries.GetEventsPage;

public record GetEventPageResponse
{
    public int TotalCountEvents { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public required ReadOnlyMemory<GetEventByIdResponse> Events { get; set; }
}