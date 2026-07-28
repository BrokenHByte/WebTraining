using Application.Abstractions.Persistence.Repositories;
using Domain.Entities;
using Infrastructure.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

public class BookingBackgroundServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;

    private readonly Guid _eventGuid1 = Guid.NewGuid();
    private readonly Guid _eventGuid2 = Guid.NewGuid();
    private readonly Mock<IEventRepository> _eventServiceMock;
    private readonly BookingBackgroundService _service;
    private readonly List<Booking> _testBookings;

    public BookingBackgroundServiceTests()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        _eventServiceMock = new Mock<IEventRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        var loggerMock = new Mock<ILogger<BookingBackgroundService>>();

        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IEventRepository)))
            .Returns(_eventServiceMock.Object);

        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IBookingRepository)))
            .Returns(_bookingRepositoryMock.Object);

        _testBookings = new List<Booking>
        {
            new() { Id = Guid.NewGuid(), EventId = _eventGuid1, Status = Booking.BookingStatus.Pending },
            new() { Id = Guid.NewGuid(), EventId = _eventGuid2, Status = Booking.BookingStatus.Pending }
        };

        _service = new BookingBackgroundService(
            scopeFactoryMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task StatusUpdateTest()
    {
        var cts = new CancellationTokenSource();
        var event1 = new Event
        {
            Id = _eventGuid1,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow,
            AvailableSeats = 10
        };

        _bookingRepositoryMock.Setup(x => x.GetPending()).Returns(_testBookings.AsQueryable());
        _eventServiceMock.Setup(x => x.GetByIdAsync(_eventGuid1)).ReturnsAsync(event1);
        _bookingRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Booking>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync(cts.Token);
        await Task.Delay(3000);
        cts.CancelAfter(100);

        Assert.Equal(Booking.BookingStatus.Confirmed, _testBookings[0].Status);
        Assert.Equal(Booking.BookingStatus.Confirmed, _testBookings[1].Status);
    }
}