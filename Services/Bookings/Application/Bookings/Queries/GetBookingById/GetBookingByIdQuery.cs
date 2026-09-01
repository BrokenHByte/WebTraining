using MediatR;

namespace Bookings.Application.Bookings.Queries.GetBookingById;

public sealed record GetBookingByIdQuery : IRequest<GetBookingByIdResponse>
{
    public Guid Id { get; init; }
    public required string UserId { get; init; }
    public required string UserRole { get; init; }
}