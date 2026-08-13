using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Application.Bookings.Commands.DeleteBooking;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests;

public class TestCancelBookingHandler
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly CancelBookingHandler _handler;

    public TestCancelBookingHandler()
    {
        _userServiceMock = new Mock<IUserService>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _handler = new CancelBookingHandler(
            _userServiceMock.Object,
            _eventRepositoryMock.Object,
            _bookingRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_AdminDeletesBooking_ShouldReleaseSeats()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userLogin = "admin@test.com";
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            EventId = eventId, 
            UserId = userId 
        };
        
        var user = new User
        {
            Id = userId,
            Login = userLogin,
            Role = User.Roles.Admin,
            HashPass = "123"
        };
        
        var @event = new Event 
        { 
            Id = eventId, 
            Title = "Test",
            AvailableSeats = 10 
        };

        _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);
        
        _userServiceMock.Setup(x => x.Get(userLogin))
            .ReturnsAsync(user);
        
        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(@event);

        var command = new CancelBookingCommand 
        { 
            Id = bookingId, 
            UserLogin = userLogin 
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _bookingRepositoryMock.Verify(x => x.CancelledByIdAsync(bookingId), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        @event.AvailableSeats.Should().Be(11); // 10 + 1 released seat
    }

    [Fact]
    public async Task Handle_UserDeletesOwnBooking_ShouldReleaseSeats()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userLogin = "user@test.com";
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            EventId = eventId, 
            UserId = userId 
        };
        
        var user = new User 
        { 
            Id = userId, 
            Login = userLogin,
            HashPass = "123",
            Role = User.Roles.User 
        };
        
        var @event = new Event 
        { 
            Id = eventId, 
            Title = "Test",
            AvailableSeats = 5 
        };

        _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);
        
        _userServiceMock.Setup(x => x.Get(userLogin))
            .ReturnsAsync(user);
        
        _eventRepositoryMock.Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(@event);

        var command = new CancelBookingCommand 
        { 
            Id = bookingId, 
            UserLogin = userLogin 
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _bookingRepositoryMock.Verify(x => x.CancelledByIdAsync(bookingId), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        @event.AvailableSeats.Should().Be(6); // 5 + 1 released seat
    }

    [Fact]
    public async Task Handle_UserDeletesOtherBooking_ShouldThrowInsufficientPrivilegesException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var userLogin = "user@test.com";
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            EventId = Guid.NewGuid(), 
            UserId = otherUserId // Different user
        };
        
        var user = new User 
        { 
            Id = userId, 
            Login = userLogin, 
            HashPass = "123",
            Role = User.Roles.User 
        };

        _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);
        
        _userServiceMock.Setup(x => x.Get(userLogin))
            .ReturnsAsync(user);

        var command = new CancelBookingCommand 
        { 
            Id = bookingId, 
            UserLogin = userLogin 
        };
        
        await Assert.ThrowsAsync<InsufficientPrivilegesException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _bookingRepositoryMock.Verify(x => x.DeleteByIdAsync(It.IsAny<Guid>()), Times.Never);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userLogin = "nonexistent@test.com";
        
        var booking = new Booking 
        { 
            Id = bookingId, 
            EventId = Guid.NewGuid(), 
            UserId = Guid.NewGuid() 
        };

        _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);
        
        _userServiceMock.Setup(x => x.Get(userLogin))
            .ReturnsAsync((User)null!);

        var command = new CancelBookingCommand 
        { 
            Id = bookingId, 
            UserLogin = userLogin 
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Be($"User {userLogin} not found");
        
        _bookingRepositoryMock.Verify(x => x.DeleteByIdAsync(It.IsAny<Guid>()), Times.Never);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ShouldThrowNullReferenceException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userLogin = "test@test.com";

        _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync((Booking)null!);

        var command = new CancelBookingCommand 
        { 
            Id = bookingId, 
            UserLogin = userLogin 
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_BookingNotFoundButUserExists_ShouldThrowNullReferenceException()
    {
        // Arrange
        var bookingId = Guid.NewGuid();
        var userLogin = "test@test.com";
        var user = new User 
        { 
            Id = Guid.NewGuid(), 
            Login = userLogin,
            HashPass = "123",
            Role = User.Roles.Admin 
        };

        _bookingRepositoryMock.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync((Booking)null!);
        
        _userServiceMock.Setup(x => x.Get(userLogin))
            .ReturnsAsync(user);

        var command = new CancelBookingCommand 
        { 
            Id = bookingId, 
            UserLogin = userLogin 
        };

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}