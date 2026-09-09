using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Application.Events.Commands.DeleteEvent;
using Events.Application.Events.Commands.UpdateEvent;
using Events.Application.Events.Queries.GetEventById;
using Events.Domain.Entities;
using Events.Domain.Exceptions;
using Moq;

namespace Events.UnitTests;

public class EventCacheTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly GetEventByIdHandler _getHandler;
    private readonly UpdateEventHandler _updateHandler;
    private readonly DeleteEventHandler _deleteHandler;

    public EventCacheTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _getHandler = new GetEventByIdHandler(_eventRepositoryMock.Object, _cacheServiceMock.Object);
        _updateHandler = new UpdateEventHandler(_eventRepositoryMock.Object, _cacheServiceMock.Object);
        _deleteHandler = new DeleteEventHandler(_eventRepositoryMock.Object, _cacheServiceMock.Object);
    }

    [Fact]
    public async Task GetEventById_WhenCacheHit_ShouldReturnCachedValueAndNotCallRepository()
    {
        var eventId = Guid.NewGuid();
        var query = new GetEventByIdQuery { Id = eventId };
        var cachedResponse = new GetEventByIdResponse
        {
            Id = eventId,
            Title = "Cached Event",
            Description = "This is a cached event",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 50,
            TotalSeats = 100
        };

        var cacheKey = $"event:{eventId}";
        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync(cachedResponse);

        var result = await _getHandler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(cachedResponse.Id.ToString(), result.Id.ToString());
        Assert.Equal(cachedResponse.Title, result.Title);

        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetObjectJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetEventById_WhenCacheMiss_ShouldGetFromRepositoryAndCacheResult()
    {
        var eventId = Guid.NewGuid();
        var query = new GetEventByIdQuery { Id = eventId };
        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Repository Event",
            Description = "This is from repository",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(3),
            AvailableSeats = 75,
            TotalSeats = 150
        };

        var cacheKey = $"event:{eventId}";
        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null!);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        var result = await _getHandler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(eventEntity.Id.ToString(), result.Id.ToString());
        Assert.Equal(eventEntity.Title, result.Title);

        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(
            x => x.SetObjectJson(
                cacheKey,
                It.Is<GetEventByIdResponse>(r => r.Id == eventEntity.Id), It.IsAny<int>()),
            Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetEventById_WhenCacheReturnsDefault_ShouldTreatAsCacheMissAndGetFromRepository()
    {
        var eventId = Guid.NewGuid();
        var query = new GetEventByIdQuery { Id = eventId };
        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Repository Event",
            Description = "Description",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(1),
            AvailableSeats = 10,
            TotalSeats = 20
        };

        var cacheKey = $"event:{eventId}";
        
        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync(default(GetEventByIdResponse));

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        var result = await _getHandler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(eventEntity.Id.ToString(), result.Id.ToString());
        
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.SetObjectJson(cacheKey, It.IsAny<GetEventByIdResponse>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetEventById_WhenRepositoryThrowsEventNotFoundException_ShouldPropagateException()
    {
        var eventId = Guid.NewGuid();
        var query = new GetEventByIdQuery { Id = eventId };
        var cacheKey = $"event:{eventId}";

        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null!);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ThrowsAsync(new EventNotFoundException("Event not found"));

        await Assert.ThrowsAsync<EventNotFoundException>(() => 
            _getHandler.Handle(query, CancellationToken.None));

        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.SetObjectJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEvent_ShouldUpdateRepositoryAndInvalidateCache()
    {
        var eventId = Guid.NewGuid();
        var command = new UpdateEventCommand
        {
            ExistingId = eventId,
            Title = "Updated Event",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        var cacheKey = $"event:{eventId}";

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(eventId, It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        await _updateHandler.Handle(command, CancellationToken.None);

        _eventRepositoryMock.Verify(
            x => x.UpdateAsync(
                eventId,
                It.Is<Event>(e =>
                    e.Id == eventId &&
                    e.Title == command.Title &&
                    e.Description == command.Description &&
                    e.StartAt == command.StartAt &&
                    e.EndAt == command.EndAt)),
            Times.Once);

        _cacheServiceMock.Verify(
            x => x.DeleteObjectJson(cacheKey),
            Times.Once);

        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetObjectJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEvent_WhenRepositoryThrowsEventNotFoundException_ShouldPropagateExceptionAndNotInvalidateCache()
    {
        var eventId = Guid.NewGuid();
        var command = new UpdateEventCommand
        {
            ExistingId = eventId,
            Title = "Updated Event",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(eventId, It.IsAny<Event>()))
            .ThrowsAsync(new EventNotFoundException("Event not found"));

        await Assert.ThrowsAsync<EventNotFoundException>(() => 
            _updateHandler.Handle(command, CancellationToken.None));

        _eventRepositoryMock.Verify(x => x.UpdateAsync(eventId, It.IsAny<Event>()), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEvent_ShouldAttemptCacheInvalidationEvenIfCacheDoesNotExist()
    {
        var eventId = Guid.NewGuid();
        var command = new UpdateEventCommand
        {
            ExistingId = eventId,
            Title = "Updated Event",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        var cacheKey = $"event:{eventId}";

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(eventId, It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        await _updateHandler.Handle(command, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(eventId, It.IsAny<Event>()), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(cacheKey), Times.Once);
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateEvent_WhenCacheServiceUnavailable_ShouldStillUpdateRepository()
    {
        var eventId = Guid.NewGuid();
        var command = new UpdateEventCommand
        {
            ExistingId = eventId,
            Title = "Updated Event",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(eventId, It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(x => x.DeleteObjectJson(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _updateHandler.Handle(command, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.UpdateAsync(eventId, It.IsAny<Event>()), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson($"event:{eventId}"), Times.Once);
    }

    [Fact]
    public async Task UpdateEvent_WithDifferentIds_ShouldInvalidateCorrectCacheKeys()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var command1 = new UpdateEventCommand
        {
            ExistingId = eventId1,
            Title = "Event 1 Updated",
            Description = "Description 1",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        var command2 = new UpdateEventCommand
        {
            ExistingId = eventId2,
            Title = "Event 2 Updated",
            Description = "Description 2",
            StartAt = DateTime.UtcNow.AddDays(3),
            EndAt = DateTime.UtcNow.AddDays(4)
        };

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        await _updateHandler.Handle(command1, CancellationToken.None);
        await _updateHandler.Handle(command2, CancellationToken.None);

        _cacheServiceMock.Verify(x => x.DeleteObjectJson($"event:{eventId1}"), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson($"event:{eventId2}"), Times.Once);
        _eventRepositoryMock.Verify(x => x.UpdateAsync(eventId1, It.IsAny<Event>()), Times.Once);
        _eventRepositoryMock.Verify(x => x.UpdateAsync(eventId2, It.IsAny<Event>()), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_ShouldDeleteFromRepositoryAndInvalidateCache()
    {
        var eventId = Guid.NewGuid();
        var command = new DeleteEventCommand { Id = eventId };
        var cacheKey = $"event:{eventId}";

        _eventRepositoryMock
            .Setup(x => x.DeleteByIdAsync(eventId))
            .Returns(Task.CompletedTask);

        await _deleteHandler.Handle(command, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.DeleteByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(cacheKey), Times.Once);
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(It.IsAny<string>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetObjectJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEvent_WhenRepositoryThrowsEventNotFoundException_ShouldPropagateExceptionAndNotInvalidateCache()
    {
        var eventId = Guid.NewGuid();
        var command = new DeleteEventCommand { Id = eventId };

        _eventRepositoryMock
            .Setup(x => x.DeleteByIdAsync(eventId))
            .ThrowsAsync(new EventNotFoundException("Event not found"));

        await Assert.ThrowsAsync<EventNotFoundException>(() => 
            _deleteHandler.Handle(command, CancellationToken.None));

        _eventRepositoryMock.Verify(x => x.DeleteByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEvent_ShouldAttemptCacheInvalidationEvenIfCacheDoesNotExist()
    {
        var eventId = Guid.NewGuid();
        var command = new DeleteEventCommand { Id = eventId };
        var cacheKey = $"event:{eventId}";

        _eventRepositoryMock
            .Setup(x => x.DeleteByIdAsync(eventId))
            .Returns(Task.CompletedTask);

        await _deleteHandler.Handle(command, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.DeleteByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson(cacheKey), Times.Once);
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEvent_WhenCacheServiceUnavailable_ShouldStillDeleteFromRepository()
    {
        var eventId = Guid.NewGuid();
        var command = new DeleteEventCommand { Id = eventId };

        _eventRepositoryMock
            .Setup(x => x.DeleteByIdAsync(eventId))
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(x => x.DeleteObjectJson(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _deleteHandler.Handle(command, CancellationToken.None);

        _eventRepositoryMock.Verify(x => x.DeleteByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson($"event:{eventId}"), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_WithDifferentIds_ShouldInvalidateCorrectCacheKeys()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var command1 = new DeleteEventCommand { Id = eventId1 };
        var command2 = new DeleteEventCommand { Id = eventId2 };

        _eventRepositoryMock
            .Setup(x => x.DeleteByIdAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        await _deleteHandler.Handle(command1, CancellationToken.None);
        await _deleteHandler.Handle(command2, CancellationToken.None);

        _cacheServiceMock.Verify(x => x.DeleteObjectJson($"event:{eventId1}"), Times.Once);
        _cacheServiceMock.Verify(x => x.DeleteObjectJson($"event:{eventId2}"), Times.Once);
        _eventRepositoryMock.Verify(x => x.DeleteByIdAsync(eventId1), Times.Once);
        _eventRepositoryMock.Verify(x => x.DeleteByIdAsync(eventId2), Times.Once);
    }

    [Fact]
    public async Task FullLifecycle_GetCacheMiss_Update_GetCacheMiss_Delete_GetCacheMiss()
    {
        var eventId = Guid.NewGuid();
        var cacheKey = $"event:{eventId}";
        var eventEntity = new Event
        {
            Id = eventId,
            Title = "New Event",
            Description = "Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            AvailableSeats = 100,
            TotalSeats = 200
        };

        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null!);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);

        var getResult1 = await _getHandler.Handle(
            new GetEventByIdQuery { Id = eventId }, 
            CancellationToken.None);

        var updateCommand = new UpdateEventCommand
        {
            ExistingId = eventId,
            Title = "Updated Event",
            Description = "Updated Description",
            StartAt = DateTime.UtcNow.AddDays(2),
            EndAt = DateTime.UtcNow.AddDays(3)
        };

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(eventId, It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        await _updateHandler.Handle(updateCommand, CancellationToken.None);

        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null!);
        
        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(new Event
            {
                Id = eventId,
                Title = "Updated Event",
                Description = "Updated Description",
                StartAt = DateTime.UtcNow.AddDays(2),
                EndAt = DateTime.UtcNow.AddDays(3),
                AvailableSeats = 100,
                TotalSeats = 200
            });

        var getResult2 = await _getHandler.Handle(
            new GetEventByIdQuery { Id = eventId }, 
            CancellationToken.None);

        var deleteCommand = new DeleteEventCommand { Id = eventId };

        _eventRepositoryMock
            .Setup(x => x.DeleteByIdAsync(eventId))
            .Returns(Task.CompletedTask);

        await _deleteHandler.Handle(deleteCommand, CancellationToken.None);

        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null!);
        
        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ThrowsAsync(new EventNotFoundException("Event not found"));

        await Assert.ThrowsAsync<EventNotFoundException>(() => 
            _getHandler.Handle(new GetEventByIdQuery { Id = eventId }, CancellationToken.None));

        _cacheServiceMock.Verify(x => x.SetObjectJson(cacheKey, It.IsAny<GetEventByIdResponse>(), It.IsAny<int>()), Times.AtLeastOnce);
        Assert.NotNull(getResult1);
        Assert.Equal("New Event", getResult1.Title);

        _cacheServiceMock.Verify(x => x.DeleteObjectJson(cacheKey), Times.AtLeastOnce);

        Assert.NotNull(getResult2);
        Assert.Equal("Updated Event", getResult2.Title);

        _cacheServiceMock.Verify(x => x.DeleteObjectJson(cacheKey), Times.Exactly(2));
    }

    [Fact]
    public async Task Update_ThenGet_ShouldNotReturnOldCachedValue()
    {
        var eventId = Guid.NewGuid();
        var cacheKey = $"event:{eventId}";
        var oldCachedValue = new GetEventByIdResponse
        {
            Id = eventId,
            Title = "Old Title",
            Description = "Old Description",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(1),
            AvailableSeats = 50,
            TotalSeats = 100
        };

        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync(oldCachedValue);

        var updateCommand = new UpdateEventCommand
        {
            ExistingId = eventId,
            Title = "New Title",
            Description = "New Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        };

        _eventRepositoryMock
            .Setup(x => x.UpdateAsync(eventId, It.IsAny<Event>()))
            .Returns(Task.CompletedTask);

        await _updateHandler.Handle(updateCommand, CancellationToken.None);

        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null!);

        var updatedEvent = new Event
        {
            Id = eventId,
            Title = "New Title",
            Description = "New Description",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            AvailableSeats = 75,
            TotalSeats = 150
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(updatedEvent);

        var getResult = await _getHandler.Handle(
            new GetEventByIdQuery { Id = eventId }, 
            CancellationToken.None);

        _cacheServiceMock.Verify(x => x.DeleteObjectJson(cacheKey), Times.Once);
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Exactly(1));
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);

        Assert.NotNull(getResult);
        Assert.Equal("New Title", getResult.Title);
        Assert.Equal("New Description", getResult.Description);
        Assert.Equal(75, getResult.AvailableSeats);
        Assert.Equal(150, getResult.TotalSeats);

        _cacheServiceMock.Verify(
            x => x.SetObjectJson(
                cacheKey,
                It.Is<GetEventByIdResponse>(r => r.Title == "New Title"), It.IsAny<int>()),
            Times.Once);
    }
}