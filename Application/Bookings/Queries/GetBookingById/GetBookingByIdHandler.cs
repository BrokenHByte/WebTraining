using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using MediatR;

namespace Application.Bookings.Queries.GetBookingById;

public class GetBookingByIdHandler(IBookingService bookingService) : IRequestHandler<GetBookingByIdQuery, GetBookingByIdResponse>
{
    public async Task<GetBookingByIdResponse> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.GetByIdAsync(request.Id);
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