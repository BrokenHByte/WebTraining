using Bookings.Application.Abstractions.Persistence.Repositories;
using Bookings.Application.Common.Config;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bookings.Application.Bookings.Commands.CreateBooking;

public class CreateBookingHandler(IBookingRepository bookingRepository, IOptions<BookingSettings> bookingOptions) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
  /*  public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var eventOne = await eventRepository.GetByIdAsync(request.EventId);
        if (DateTime.UtcNow > eventOne.StartAt)
        {
            throw new BookingBeginEventException("The event has already been started");
        }

        var user = await userService.Get(request.UserLogin);
        if (user == null)
            throw new InvalidOperationException($"User {request.UserLogin} not found");

        await BookingLock.ExecuteAsync(async () =>
        {
            var existBookings = bookingRepository
                .GetBookingsByUser(user.Id)
                .Where(x => x.Status == Booking.BookingStatus.Pending || x.Status == Booking.BookingStatus.Confirmed).ToList();
            if (existBookings.Count >= bookingOptions.Value.PerUserLimit)
            {
                throw new BookingExceedingLimitException($"The maximum number of bookings exceeded. (Limit {bookingOptions.Value.PerUserLimit})");
            }
            if (!eventOne.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");
        }, cancellationToken);

        var result = await bookingRepository.CreateAsync(request.EventId, user.Id);
        return new CreateBookingResponse { Id = result.Id, EventId = result.EventId, Status = result.Status };
    }*/
  public Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
  {
      throw new NotImplementedException();
  }
}