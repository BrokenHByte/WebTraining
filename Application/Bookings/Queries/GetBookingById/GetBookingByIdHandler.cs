using Application.Abstractions.Persistence.Repositories;
using MediatR;

namespace Application.Bookings.Queries.GetBookingById;

public class GetBookingByIdHandler(IBookingRepository bookingRepository) : IRequestHandler<GetBookingByIdQuery, GetBookingByIdResponse>
{
    public async Task<GetBookingByIdResponse> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.Id);
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