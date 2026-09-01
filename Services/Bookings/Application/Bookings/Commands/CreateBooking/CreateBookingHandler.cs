using Bookings.Application.Abstractions.Persistence.Repositories;
using Bookings.Application.Common.Config;
using Bookings.Application.Common.Locks;
using Bookings.Domain.Entities;
using Bookings.Domain.Exceptions;
using Contracts.Kafka;
using Contracts.Messages;
using MediatR;
using Microsoft.Extensions.Options;

namespace Bookings.Application.Bookings.Commands.CreateBooking;

public class CreateBookingHandler(KafkaProducerService kafkaProducerService, IBookingRepository bookingRepository, IOptions<BookingSettings> bookingOptions) : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{

    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var existBookings = bookingRepository.GetBookingsByUser(new Guid(request.UserId)).Where(x => x.Status == Booking.BookingStatus.Pending || x.Status == Booking.BookingStatus.Confirmed).ToList();
        if (existBookings.Count >= bookingOptions.Value.PerUserLimit)
        {
            throw new BookingExceedingLimitException($"The maximum number of bookings exceeded. (Limit {bookingOptions.Value.PerUserLimit})");
        }

        var result = await bookingRepository.CreateAsync(new Guid(request.EventId), new Guid(request.UserId));
        await kafkaProducerService.SendAsync<CreateBookingMessage>(TopicNames.BookingCreate, result.EventId, new CreateBookingMessage()
        {
            BookingId = result.Id.ToString(),
            EventId = result.EventId.ToString(),
            UserId = result.UserId.ToString()
        });
        
        return new CreateBookingResponse { Id = result.Id, EventId = result.EventId, Status = result.Status };
    }
}