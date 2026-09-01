using Bookings.Application.Abstractions.Persistence.Repositories;
using Bookings.Application.Bookings.Commands.CompletingBooking;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Bookings.Commands.RejectBooking;

public class RejectBookingHandler(IBookingRepository bookingRepository, ILogger<CompletingBookingHandler> logger) : IRequestHandler<RejectBookingCommand>
{
      public async Task Handle(RejectBookingCommand request, CancellationToken cancellationToken)
      {
          var booking = await bookingRepository.GetByIdAsync(request.BookingId);
          await bookingRepository.UpdateAsync(request.BookingId, booking.Reject());
          Console.WriteLine(request.Error);
      }
}