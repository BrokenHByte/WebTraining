using MediatR;

namespace Application.Bookings.Queries.GetBookingById;

public sealed record GetBookingByIdQuery : IRequest<GetBookingByIdResponse>
{
    public Guid Id { get; init; }
}