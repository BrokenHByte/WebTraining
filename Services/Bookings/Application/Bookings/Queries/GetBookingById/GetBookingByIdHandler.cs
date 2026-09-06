using Bookings.Application.Abstractions.Persistence.Repositories;
using Bookings.Domain.Exceptions;
using MediatR;

namespace Bookings.Application.Bookings.Queries.GetBookingById;


public class GetBookingByIdHandler(IBookingRepository bookingRepository) : IRequestHandler<GetBookingByIdQuery, GetBookingByIdResponse>
{
    public async Task<GetBookingByIdResponse> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.Id);
        if (request.UserRole == "User" && booking.UserId.ToString() != request.UserId)
        {
            throw new InsufficientPrivilegesException("You do not have permission to get this booking");
        }

        return new GetBookingByIdResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}
