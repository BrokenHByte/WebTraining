using Application.Abstractions.Persistence.Repositories;
using Domain.Entities;
using Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Bookings.Commands.CompletingBooking;

public class CompletingBookingHandler(IEventRepository eventRepository, IBookingRepository bookingRepository, ILogger<CompletingBookingHandler> logger) : IRequestHandler<CompletingBookingCommand>
{
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    
    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);
        Event? existedEvent = null;
        var cloneBooking = (Booking)booking.Clone();

        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            await _processingSemaphore.WaitAsync(stoppingToken);
            existedEvent = await eventRepository.GetByIdAsync(cloneBooking.EventId);
            await bookingRepository.UpdateAsync(booking.Id, cloneBooking.Confirm());
        }
        catch (EventNotFoundException)
        {
            await bookingRepository.UpdateAsync(booking.Id, cloneBooking.Reject());
            logger.LogWarning($"Booking {cloneBooking.EventId} rejected. Event not found");
        }
        catch (OperationCanceledException)
        {
            // Остановка сервиса. Вероятно штатная ситуация
        }
        catch (Exception)
        {
            await bookingRepository.UpdateAsync(booking.Id, cloneBooking.Reject());
            if (existedEvent != null) existedEvent.ReleaseSeats();
            logger.LogWarning($"Booking {cloneBooking.EventId} rejected. ");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
    
    public async Task Handle(CompletingBookingCommand request, CancellationToken cancellationToken)
    {
        var pendingBookings = bookingRepository.GetPending().ToList();
        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, cancellationToken));

        await Task.WhenAll(tasks);
        if (pendingBookings.Count > 0)
            logger.LogInformation($"Booking {pendingBookings.Count} bookings updated.");
    }
}