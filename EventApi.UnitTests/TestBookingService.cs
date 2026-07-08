using Microsoft.Extensions.Logging;
using Moq;
using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Repositories;
using WebProject.Services;

namespace UnitTests;

public class BookingServiceTests
{
    private readonly Guid _guidEvent = Guid.NewGuid();
    private readonly Mock<IEventService> _mockEventService = new();
    private readonly Mock<ILogger<BookingService>> _mockLogger = new();
    private readonly Mock<IBookingRepository> _mockRepository = new();

    [Fact]
    public async Task CreateAsync()
    {
        _mockEventService.Reset();
        _mockRepository.Reset();
        var reserveEvent = new Event
        {
            Id = _guidEvent,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.Today,
            EndAt = DateTime.Today.AddDays(1),
            TotalSeats = 1,
            AvailableSeats = 1
        };

        _mockEventService.Setup(repo => repo.GetByIdAsync(_guidEvent))
            .ReturnsAsync(reserveEvent);

        var service = new BookingService(_mockRepository.Object, _mockEventService.Object, _mockLogger.Object);
        await service.CreateAsync(_guidEvent);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await service.CreateAsync(_guidEvent));
    }

    [Fact]
    public async Task DeleteAsync()
    {
        _mockEventService.Reset();
        _mockRepository.Reset();
        var reserveEvent = new Event
        {
            Id = _guidEvent,
            Title = "Test",
            Description = "Test",
            StartAt = DateTime.Today,
            EndAt = DateTime.Today.AddDays(1),
            TotalSeats = 1,
            AvailableSeats = 1
        };

        var reserveBooking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = reserveEvent.Id,
            CreatedAt = DateTime.Today,
            ProcessedAt = DateTime.Today.AddDays(1),
            Status = Booking.BookingStatus.Pending
        };

        _mockEventService.Setup(repo => repo.GetByIdAsync(_guidEvent))
            .ReturnsAsync(reserveEvent);

        _mockRepository.Setup(repo => repo.GetByIdAsync(reserveBooking.Id))
            .ReturnsAsync(reserveBooking);

        var service = new BookingService(_mockRepository.Object, _mockEventService.Object, _mockLogger.Object);
        await service.CreateAsync(_guidEvent);
        Assert.Equal(0, reserveEvent.AvailableSeats);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await service.CreateAsync(_guidEvent));
        await service.DeleteByIdAsync(reserveBooking.Id);
        Assert.Equal(1, reserveEvent.AvailableSeats);
    }
}