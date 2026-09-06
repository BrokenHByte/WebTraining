using Bookings.Application.Abstractions.Persistence.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Bookings.Commands.CompletingBooking;

public class CompletingBookingHandler(IBookingRepository bookingRepository, ILogger<CompletingBookingHandler> logger) : IRequestHandler<CompletingBookingCommand>
{
    public async Task Handle(CompletingBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId);
        await bookingRepository.UpdateAsync(request.BookingId, booking.Confirm());
    }
}