using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Application.Common.Locks;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;

namespace Application.Bookings.Commands.DeleteBooking;

public class CancelBookingHandler(IUserService userService, IEventRepository eventRepository, IBookingRepository bookingRepository) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        await BookingLock.ExecuteAsync(async () =>
        {
            var oneBooking = await bookingRepository.GetByIdAsync(request.Id);
            var user = await userService.Get(request.UserLogin);
            if (user == null)
                throw new InvalidOperationException($"User {request.UserLogin} not found");

            if (user.Role == User.Roles.Admin || (user.Role == User.Roles.User && oneBooking.UserId == user.Id))
            {
                var oneEvent = await eventRepository.GetByIdAsync(oneBooking.EventId);
                if (oneEvent.StartAt < DateTime.UtcNow)
                {
                    throw new BookingBeginEventException("The event has already started.");
                }
                oneEvent.ReleaseSeats();
                await bookingRepository.CancelledByIdAsync(oneBooking.Id);
            }
            else
            if (user.Role == User.Roles.User)
            {
                throw new InsufficientPrivilegesException("You do not have permission to delete this booking");
            }
        }, cancellationToken);
    }
}