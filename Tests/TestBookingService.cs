using Microsoft.Extensions.Logging;
using Moq;
using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Services;

namespace Tests;

public class TestBookingService
{
    [Fact]
    public void TestBooking()
    {
        var mockLogger1 = new Mock<ILogger<EventService>>();
        var mockLogger2 = new Mock<ILogger<BookingService>>();
        IEventService eventService = new EventService(mockLogger1.Object);
        IBookingService bookingService = new BookingService(eventService, mockLogger2.Object);
        List<Guid> guidsEvent = new();
        foreach (var ev in EventData.ExpectedTestData())
            guidsEvent.Add(eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt));

        // Создаём невалидное
        var err = Assert.Throws<EventNotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.Empty).Wait());
        Assert.Equal("Event not found", err.Message);

        // Создаём несколько броней на одно событие
        bookingService.CreateBookingAsync(guidsEvent[0]);
        bookingService.CreateBookingAsync(guidsEvent[0]);
        var bookings = bookingService.GetBookings().ToArray();
        Assert.Equal(2, bookings.Length);
        Assert.NotEqual(bookings[0].Id, bookings[1].Id);
        Assert.Equal(Booking.BookingStatus.Pending, bookings[0].Status);
        Assert.Equal(Booking.BookingStatus.Pending, bookings[1].Status);

        // Получаем Booking по Id
        var booking = bookingService.GetBookingByIdAsync(bookings[0].Id).Result;
        Assert.Equal(booking.Id, bookings[0].Id);
        Assert.Equal(booking.EventId, bookings[0].EventId);
        Assert.Equal(booking.Status, bookings[0].Status);
        Assert.Equal(booking.CreatedAt, bookings[0].CreatedAt);
        Assert.Equal(booking.ProcessedAt, bookings[0].ProcessedAt);

        // Получаем бронь по невалидному ID
        var errBooking = Assert.Throws<BookingNotFoundException>(() =>
            bookingService.GetBookingByIdAsync(Guid.Empty).Wait());
        Assert.Equal("Booking not found", errBooking.Message);

        // Проверка обновления
        var processedAt = DateTime.UtcNow;
        var updatedBooking = booking with
        {
            Status = Booking.BookingStatus.Confirmed, ProcessedAt = processedAt
        };
        bookingService.UpdateBooking(booking.Id, updatedBooking);
        booking = bookingService.GetBookingByIdAsync(bookings[0].Id).Result;
        Assert.Equal(Booking.BookingStatus.Confirmed, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);

        // Создаём бронь на удалённое событие
        eventService.DeleteEventById(guidsEvent[0]);
        err = Assert.Throws<EventNotFoundException>(() =>
            bookingService.CreateBookingAsync(guidsEvent[0]).Wait());
        Assert.Equal("Event not found", err.Message);
    }
}