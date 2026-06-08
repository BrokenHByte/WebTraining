namespace WebProject.Services;

// Временное решение, каскадное удаление брони при удалении события
public interface IEventCoordinationService
{
    Task DeleteEventWithCheck(Guid eventId);
}

public class EventCoordinationService(
    IBookingService bookingService,
    IEventService eventService,
    ILogger<EventCoordinationService> logger)
    : IEventCoordinationService
{
    public Task DeleteEventWithCheck(Guid eventId)
    {
        // Проверяем наличие броней для события
        var booking = bookingService.GetBookings().Where(x => x.EventId == eventId).ToArray();
        logger.LogInformation($"{booking.Length} event bookings were deleted");
        foreach (var item in booking)
            bookingService.DeleteBookingById(item.Id);
        eventService.DeleteEventById(eventId);
        return Task.CompletedTask;
    }
}