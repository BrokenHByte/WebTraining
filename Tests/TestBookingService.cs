using Microsoft.Extensions.Logging;
using Moq;
using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Services;

namespace Tests;

public class TestBookingService
{
    [Fact]
    public async Task TestBooking()
    {
        var mockLogger1 = new Mock<ILogger<EventService>>();
        var mockLogger2 = new Mock<ILogger<BookingService>>();
        IEventService eventService = new EventService(mockLogger1.Object);
        IBookingService bookingService = new BookingService(eventService, mockLogger2.Object);
        List<Guid> guidsEvent = new();
        foreach (var ev in EventData.ExpectedTestData())
            guidsEvent.Add(await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 2));

        // Создаём невалидное
        var err = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await bookingService.CreateBookingAsync(Guid.Empty));

        Assert.Equal("Event not found", err.Message);

        // Создаём несколько броней на одно событие
        await bookingService.CreateBookingAsync(guidsEvent[0]);
        await bookingService.CreateBookingAsync(guidsEvent[0]);
        var bookings = bookingService.GetBookings().ToArray();
        Assert.Equal(2, bookings.Length);
        Assert.NotEqual(bookings[0].Id, bookings[1].Id);
        Assert.Equal(Booking.BookingStatus.Pending, bookings[0].Status);
        Assert.Equal(Booking.BookingStatus.Pending, bookings[1].Status);

        // Получаем Booking по Id
        var booking = await bookingService.GetBookingByIdAsync(bookings[0].Id);
        Assert.Equal(booking.Id, bookings[0].Id);
        Assert.Equal(booking.EventId, bookings[0].EventId);
        Assert.Equal(booking.Status, bookings[0].Status);
        Assert.Equal(booking.CreatedAt, bookings[0].CreatedAt);
        Assert.Equal(booking.ProcessedAt, bookings[0].ProcessedAt);

        // Получаем бронь по невалидному ID
        var errBooking = await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            await bookingService.GetBookingByIdAsync(Guid.Empty));

        Assert.Equal("Booking not found", errBooking.Message);

        // Проверка обновления
        var processedAt = DateTime.UtcNow;
        var updatedBooking = (Booking)booking.Clone();
        updatedBooking.Confirm();
        updatedBooking.ProcessedAt = processedAt;

        bookingService.UpdateBooking(booking.Id, updatedBooking);
        booking = await bookingService.GetBookingByIdAsync(bookings[0].Id);
        Assert.Equal(Booking.BookingStatus.Confirmed, booking.Status);
        Assert.Equal(processedAt, booking.ProcessedAt);

        // Создаём бронь на удалённое событие
        await eventService.DeleteEventByIdAsync(guidsEvent[0]);
        err = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await bookingService.CreateBookingAsync(guidsEvent[0]));

        Assert.Equal("Event not found", err.Message);
    }


    [Fact]
    public async Task TestBookingBackgroundService()
    {
        var mockBooking = new Mock<IBookingService>();
        var mockEvent = new Mock<IEventService>();
        var returnBookings = new List<Booking>
        {
            new()
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = Booking.BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            }
        };

        mockBooking.Setup(service => service.GetBookings()).Returns(returnBookings);
        mockBooking.Setup(service => service.UpdateBooking(It.IsAny<Guid>(), It.IsAny<Booking>()))
            .Callback<Guid, Booking>((id, booking) => { returnBookings.Add(booking); });


        var mockLogger = new Mock<ILogger<BookingBackgroundService>>();
        BookingBackgroundService service = new(mockBooking.Object, mockEvent.Object, mockLogger.Object);
        using var cts = new CancellationTokenSource();
        var startTask = service.StartAsync(cts.Token);
        await startTask;
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(Booking.BookingStatus.Confirmed, returnBookings[1].Status);
    }
}