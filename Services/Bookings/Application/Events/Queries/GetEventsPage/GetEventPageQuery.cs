using MediatR;

namespace Application.Events.Queries.GetEventsPage;

public sealed record GetEventPageQuery : IRequest<GetEventPageResponse>
{
    public string? Title { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? Page { get; init; }
    public int? PageSize { get; init; }
}