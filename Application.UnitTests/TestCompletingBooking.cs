using Application.Abstractions.Persistence.Repositories;
using Application.Bookings.Commands.CompletingBooking;
using Domain.Entities;
using Infrastructure.Background;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

public class TestCompletingBooking
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;

    private readonly Guid _eventGuid1 = Guid.NewGuid();
    private readonly Guid _eventGuid2 = Guid.NewGuid();
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly List<Booking> _testBookings;

    public TestCompletingBooking()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        var loggerMock = new Mock<ILogger<TestCompletingBookingBackgroundService>>();

        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IEventRepository)))
            .Returns(_eventRepositoryMock.Object);

        scopeMock.Setup(x => x.ServiceProvider.GetService(typeof(IBookingRepository)))
            .Returns(_bookingRepositoryMock.Object);

        _testBookings = new List<Booking>
        {
            new() { Id = Guid.NewGuid(), EventId = _eventGuid1, Status = Booking.BookingStatus.Pending }
        };
        
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
        _eventRepositoryMock.Setup(x => x.GetByIdAsync(_eventGuid1)).ReturnsAsync(event1);
        _bookingRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Booking>())).Callback<Guid, Booking>((_,_) => 
        {
            _testBookings[0].Status = Booking.BookingStatus.Confirmed;
        }).Returns(Task.CompletedTask);
        
        var loggerMock = new Mock<ILogger<CompletingBookingHandler>>();
        var handler = new CompletingBookingHandler(_eventRepositoryMock.Object, _bookingRepositoryMock.Object, loggerMock.Object);
        var command = new CompletingBookingCommand();
        
        await handler.Handle(command, CancellationToken.None);
        
        await Task.Delay(3000);
        cts.CancelAfter(100);
        Assert.Equal(Booking.BookingStatus.Confirmed, _testBookings[0].Status);
    }
}