using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Application.Bookings.Commands.CreateBooking;
using Application.Common.Config;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Tests;

public class TestCreateBookingHandler
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly IOptions<BookingSettings> _bookingOptions;
    private readonly Application.Bookings.Commands.CreateBooking.CreateBookingHandler _handler;
    private readonly Guid _testEventId;
    private readonly Guid _testUserId;
    private readonly Guid _testBookingId;

    public TestCreateBookingHandler()
    {
        _userServiceMock = new Mock<IUserService>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _bookingOptions = Options.Create(new BookingSettings { PerUserLimit = 3 });
        _handler = new Application.Bookings.Commands.CreateBooking.CreateBookingHandler(
            _userServiceMock.Object,
            _eventRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            _bookingOptions);

        _testEventId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _testBookingId = Guid.NewGuid();
    }

    [Fact]
    public async Task Handle_WhenEventIsValidAndUserExists_ShouldCreateBooking()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };
        var createdBooking = new Booking { Id = _testBookingId, EventId = _testEventId, Status = Booking.BookingStatus.Confirmed };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(new List<Booking>().AsQueryable());
        _bookingRepositoryMock.Setup(x => x.CreateAsync(command.EventId, user.Id))
            .ReturnsAsync(createdBooking);

        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.Id.Should().Be(_testBookingId);
        result.EventId.Should().Be(_testEventId);
        result.Status.Should().Be(Booking.BookingStatus.Confirmed);

        _eventRepositoryMock.Verify(x => x.GetByIdAsync(command.EventId), Times.Once);
        _userServiceMock.Verify(x => x.Get(command.UserLogin), Times.Once);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(command.EventId, user.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEventAlreadyStarted_ShouldThrowBookingBeginEventException()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddMinutes(-5), TotalSeats = 10, AvailableSeats = 10 };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);

        await Assert.ThrowsAsync<BookingBeginEventException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userServiceMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);
        _bookingRepositoryMock.Verify(x => x.GetBookingsByUser(It.IsAny<Guid>()), Times.Never);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "nonexistent"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync((User)null!);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("User nonexistent not found");

        _bookingRepositoryMock.Verify(x => x.GetBookingsByUser(It.IsAny<Guid>()), Times.Never);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExceedsBookingLimit_ShouldThrowBookingExceedingLimitException()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };

        var existingBookings = new List<Booking>
        {
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() },
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() },
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() }
        };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(existingBookings.AsQueryable());

        var exception = await Assert.ThrowsAsync<BookingExceedingLimitException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("The maximum number of bookings exceeded. (Limit 3)");

        _bookingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasLessThanLimitBookings_ShouldCreateBooking()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };
        var createdBooking = new Booking { Id = _testBookingId, EventId = _testEventId, Status = Booking.BookingStatus.Confirmed };

        var existingBookings = new List<Booking>
        {
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() },
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() }
        };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(existingBookings.AsQueryable());
        _bookingRepositoryMock.Setup(x => x.CreateAsync(command.EventId, user.Id))
            .ReturnsAsync(createdBooking);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(_testBookingId);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(command.EventId, user.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomBookingLimit_ShouldUseConfiguredLimit()
    {
        // Arrange
        var customOptions = Options.Create(new BookingSettings { PerUserLimit = 5 });
        var handler = new Application.Bookings.Commands.CreateBooking.CreateBookingHandler(
            _userServiceMock.Object,
            _eventRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            customOptions);

        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };

        var existingBookings = new List<Booking>();
        for (int i = 0; i < 5; i++)
        {
            existingBookings.Add(new Booking
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                EventId = Guid.NewGuid()
            });
        }

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(existingBookings.AsQueryable());

        var exception = await Assert.ThrowsAsync<BookingExceedingLimitException>(
            () => handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("(Limit 5)");
    }

    [Fact]
    public async Task Handle_WhenGetBookingsByUserReturnsEmptyQueryable_ShouldCreateBooking()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };
        var createdBooking = new Booking { Id = _testBookingId, EventId = _testEventId, Status = Booking.BookingStatus.Confirmed };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);

        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);

        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(Enumerable.Empty<Booking>().AsQueryable());

        _bookingRepositoryMock.Setup(x => x.CreateAsync(command.EventId, user.Id))
            .ReturnsAsync(createdBooking);

        var result = await _handler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        result.Id.Should().Be(_testBookingId);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(command.EventId, user.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasExactlyLimitBookings_ShouldThrowException()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };

        var existingBookings = new List<Booking>
        {
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() },
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() },
            new Booking { Id = Guid.NewGuid(), UserId = _testUserId, EventId = Guid.NewGuid() }
        };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(existingBookings.AsQueryable());

        var exception = await Assert.ThrowsAsync<BookingExceedingLimitException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("The maximum number of bookings exceeded. (Limit 3)");
        _bookingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEventStartAtIsExactlyNow_ShouldThrowException()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow, TotalSeats = 10, AvailableSeats = 10 };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);

        await Assert.ThrowsAsync<BookingBeginEventException>(
            () => _handler.Handle(command, CancellationToken.None));

        _userServiceMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);
        _bookingRepositoryMock.Verify(x => x.GetBookingsByUser(It.IsAny<Guid>()), Times.Never);
        _bookingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentEventsAndUsers_ShouldCreateCorrectBookings()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var bookingId1 = Guid.NewGuid();
        var bookingId2 = Guid.NewGuid();

        var command1 = new CreateBookingCommand
        {
            EventId = eventId1,
            UserLogin = "user1"
        };

        var event1 = new Event { Id = eventId1, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user1 = new User { Id = userId1, Login = "user1", HashPass = "123" };
        var createdBooking1 = new Booking { Id = bookingId1, EventId = eventId1, Status = Booking.BookingStatus.Confirmed };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId1))
            .ReturnsAsync(event1);
        _userServiceMock.Setup(x => x.Get("user1"))
            .ReturnsAsync(user1);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(userId1))
            .Returns(new List<Booking>().AsQueryable());
        _bookingRepositoryMock.Setup(x => x.CreateAsync(eventId1, userId1))
            .ReturnsAsync(createdBooking1);

        var result1 = await _handler.Handle(command1, CancellationToken.None);

        result1.Id.Should().Be(bookingId1);
        result1.EventId.Should().Be(eventId1);

        // Arrange for second booking
        var command2 = new CreateBookingCommand
        {
            EventId = eventId2,
            UserLogin = "user2"
        };

        var event2 = new Event { Id = eventId2, Title = "T", StartAt = DateTime.UtcNow.AddHours(2), TotalSeats = 10, AvailableSeats = 10 };
        var user2 = new User { Id = userId2, Login = "user2", HashPass = "123" };
        var createdBooking2 = new Booking { Id = bookingId2, EventId = eventId2, Status = Booking.BookingStatus.Confirmed };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId2))
            .ReturnsAsync(event2);
        _userServiceMock.Setup(x => x.Get("user2"))
            .ReturnsAsync(user2);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(userId2))
            .Returns(new List<Booking>().AsQueryable());
        _bookingRepositoryMock.Setup(x => x.CreateAsync(eventId2, userId2))
            .ReturnsAsync(createdBooking2);

        var result2 = await _handler.Handle(command2, CancellationToken.None);
        result2.Id.Should().Be(bookingId2);
        result2.EventId.Should().Be(eventId2);

        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId1), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId2), Times.Once);
        _userServiceMock.Verify(x => x.Get("user1"), Times.Once);
        _userServiceMock.Verify(x => x.Get("user2"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBookingCreationFails_ShouldPropagateException()
    {
        var command = new CreateBookingCommand
        {
            EventId = _testEventId,
            UserLogin = "testuser"
        };

        var eventOne = new Event { Id = _testEventId, Title = "T", StartAt = DateTime.UtcNow.AddHours(1), TotalSeats = 10, AvailableSeats = 10 };
        var user = new User { Id = _testUserId, Login = "testuser", HashPass = "123" };

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(command.EventId))
            .ReturnsAsync(eventOne);
        _userServiceMock.Setup(x => x.Get(command.UserLogin))
            .ReturnsAsync(user);
        _bookingRepositoryMock.Setup(x => x.GetBookingsByUser(user.Id))
            .Returns(new List<Booking>().AsQueryable());
        _bookingRepositoryMock.Setup(x => x.CreateAsync(command.EventId, user.Id))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be("Database error");
    }
}