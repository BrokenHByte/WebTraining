using Application.Abstractions.Persistence.Repositories;
using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Presentation.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

public class BookingServiceTests
{
    private readonly Guid _guidEvent = Guid.NewGuid();
    private readonly Mock<IEventRepository> _mockEventRepository = new();
    private readonly Mock<ILogger<BookingService>> _mockLogger = new();
    private readonly Mock<IBookingRepository> _mockBookingRepository = new();

    [Fact]
    public async Task CreateAsync()
    {
        _mockEventRepository.Reset();
        _mockBookingRepository.Reset();
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

        _mockEventRepository.Setup(repo => repo.GetByIdAsync(_guidEvent))
            .ReturnsAsync(reserveEvent);

        var service = new BookingService(_mockBookingRepository.Object, _mockEventRepository.Object, _mockLogger.Object);
        await service.CreateAsync(_guidEvent);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await service.CreateAsync(_guidEvent));
    }

    [Fact]
    public async Task DeleteAsync()
    {
        _mockEventRepository.Reset();
        _mockBookingRepository.Reset();
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

        _mockEventRepository.Setup(repo => repo.GetByIdAsync(_guidEvent))
            .ReturnsAsync(reserveEvent);

        _mockBookingRepository.Setup(repo => repo.GetByIdAsync(reserveBooking.Id))
            .ReturnsAsync(reserveBooking);

        var service = new BookingService(_mockBookingRepository.Object, _mockEventRepository.Object, _mockLogger.Object);
        await service.CreateAsync(_guidEvent);
        Assert.Equal(0, reserveEvent.AvailableSeats);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(async () => await service.CreateAsync(_guidEvent));
        await service.DeleteByIdAsync(reserveBooking.Id);
        Assert.Equal(1, reserveEvent.AvailableSeats);
    }
}