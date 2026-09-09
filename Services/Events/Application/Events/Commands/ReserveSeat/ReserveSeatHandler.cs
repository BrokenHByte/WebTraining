using Contracts.Cache;
using Contracts.Kafka;
using Contracts.Messages;
using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Application.Events.Commands.CreateEvent;
using Events.Application.Events.Common;
using Events.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Events.Application.Events.Commands.ReserveSeat;


public class ReserveSeatHandler(IEventRepository eventRepository, KafkaProducerService kafkaService, ILogger<CreateEventHandler> logger, ICacheService cacheService) : IRequestHandler<ReserveSeatCommand>
{

    public async Task Handle(ReserveSeatCommand request, CancellationToken cancellationToken)
    {
        var eventOne = await eventRepository.GetByIdAsync(new Guid(request.EventId));
        if (DateTime.UtcNow > eventOne.StartAt)
        {
            await kafkaService.SendAsync(TopicNames.BookingReject, new Guid(request.EventId),
                new RejectBookingMessage()
                {
                    BookingId = request.BookingId,
                    Error = "The event has already started",
                    CodeError = "400"
                });
            return;
        }

        if (!eventOne.TryReserveSeats())
        {
            await kafkaService.SendAsync(TopicNames.BookingReject, new Guid(request.EventId),
                new RejectBookingMessage()
                {
                    BookingId = request.BookingId,
                    Error = "No available seats for this event",
                    CodeError = "409"
                });
            return;
        }
        await eventRepository.UpdateAsync(eventOne.Id, eventOne);
        await cacheService.DeleteObjectJson( CacheKeys.KeyGetEventById + eventOne.Id.ToString());
        await kafkaService.SendAsync(TopicNames.BookingConfirmation, new Guid(request.EventId),
            new ConfirmationBookingMessage()
            {
                BookingId = request.BookingId
            });
    }
}