using Bookings.Application.Abstractions.Persistence.Repositories;
using Bookings.Domain.Exceptions;
using Contracts.Kafka;
using Contracts.Messages;
using MediatR;

namespace Bookings.Application.Bookings.Commands.DeleteBooking;

public class CancelBookingHandler(KafkaProducerService kafkaProducerService, IBookingRepository bookingRepository) : IRequestHandler<CancelBookingCommand>
{
    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
            var oneBooking = await bookingRepository.GetByIdAsync(request.Id);
            if (request.UserRole == "Admin" || (request.UserRole == "User" && oneBooking.UserId.ToString() == request.UserId))
            {
                // TODO для проверки ещё одну связку
                /*var oneEvent = await eventRepository.GetByIdAsync(oneBooking.EventId);
                if (oneEvent.StartAt < DateTime.UtcNow)
                {
                    throw new BookingBeginEventException("The event has already started.");
                } 
                oneEvent.ReleaseSeats();*/
                await bookingRepository.CancelledByIdAsync(oneBooking.Id);
                await kafkaProducerService.SendAsync<CancelledBookingMessage>(TopicNames.BookingCancelled, oneBooking.EventId, new CancelledBookingMessage()
                {
                    EventId = oneBooking.EventId.ToString()
                });
            }
            else
            if (request.UserRole == "User")
            {
                throw new InsufficientPrivilegesException("You do not have permission to delete this booking");
            }
    }
}