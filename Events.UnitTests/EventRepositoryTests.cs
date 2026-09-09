using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Application.Events.Queries.GetEventById;
using Events.Domain.Entities;
using Moq;
using StackExchange.Redis;

namespace Events.UnitTests;

public class EventRepositoryTests
{
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly GetEventByIdHandler _handler;

    public EventRepositoryTests()
    {
        _eventRepositoryMock = new Mock<IEventRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _handler = new GetEventByIdHandler(_eventRepositoryMock.Object, _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCacheExists_ShouldReturnCachedValueAndNotCallRepository()
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
        
        var result = await _handler.Handle(query, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(cachedResponse.Id, result.Id);
        Assert.Equal(cachedResponse.Title, result.Title);
        Assert.Equal(cachedResponse.Description, result.Description);
        Assert.Equal(cachedResponse.StartAt, result.StartAt);
        Assert.Equal(cachedResponse.EndAt, result.EndAt);
        Assert.Equal(cachedResponse.AvailableSeats, result.AvailableSeats);
        Assert.Equal(cachedResponse.TotalSeats, result.TotalSeats);
        
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _cacheServiceMock.Verify(x => x.SetObjectJson(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheDoesNotExist_ShouldGetFromRepositoryAndCacheResult()
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
            .ReturnsAsync((GetEventByIdResponse)null);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);
        
        var result = await _handler.Handle(query, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(eventEntity.Id, result.Id);
        Assert.Equal(eventEntity.Title, result.Title);
        Assert.Equal(eventEntity.Description, result.Description);
        Assert.Equal(eventEntity.StartAt, result.StartAt);
        Assert.Equal(eventEntity.EndAt, result.EndAt);
        Assert.Equal(eventEntity.AvailableSeats, result.AvailableSeats);
        Assert.Equal(eventEntity.TotalSeats, result.TotalSeats);
        
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        
        _cacheServiceMock.Verify(
            x => x.SetObjectJson(
                cacheKey, 
                It.Is<GetEventByIdResponse>(r => 
                    r.Id == eventEntity.Id && 
                    r.Title == eventEntity.Title &&
                    r.Description == eventEntity.Description)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheReturnsNull_ShouldNotUseCacheAndGetFromRepository()
    {
        var eventId = Guid.NewGuid();
        var query = new GetEventByIdQuery { Id = eventId };
        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(1),
            AvailableSeats = 10,
            TotalSeats = 20
        };

        var cacheKey = $"event:{eventId}";
        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey))
            .ReturnsAsync((GetEventByIdResponse)null);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(eventEntity);
        
        var result = await _handler.Handle(query, CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal(eventEntity.Id, result.Id);
        
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>(cacheKey), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _cacheServiceMock.Verify(x => x.SetObjectJson(cacheKey, It.IsAny<GetEventByIdResponse>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMultipleDifferentIds_ShouldUseDifferentCacheKeys()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var query1 = new GetEventByIdQuery { Id = eventId1 };
        var query2 = new GetEventByIdQuery { Id = eventId2 };

        var event1 = new Event
        {
            Id = eventId1,
            Title = "Event 1",
            Description = "Description 1",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(1),
            AvailableSeats = 10,
            TotalSeats = 20
        };

        var event2 = new Event
        {
            Id = eventId2,
            Title = "Event 2",
            Description = "Description 2",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddHours(2),
            AvailableSeats = 20,
            TotalSeats = 30
        };
        
        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>($"event:{eventId1}"))
            .ReturnsAsync((GetEventByIdResponse)null);
        _cacheServiceMock
            .Setup(x => x.GetObjectJson<GetEventByIdResponse>($"event:{eventId2}"))
            .ReturnsAsync((GetEventByIdResponse)null);

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId1))
            .ReturnsAsync(event1);
        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId2))
            .ReturnsAsync(event2);
        
        var result1 = await _handler.Handle(query1, CancellationToken.None);
        var result2 = await _handler.Handle(query2, CancellationToken.None);
        
        Assert.Equal(eventId1, result1.Id);
        Assert.Equal(eventId2, result2.Id);
        
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>($"event:{eventId1}"), Times.Once);
        _cacheServiceMock.Verify(x => x.GetObjectJson<GetEventByIdResponse>($"event:{eventId2}"), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId1), Times.Once);
        _eventRepositoryMock.Verify(x => x.GetByIdAsync(eventId2), Times.Once);
        _cacheServiceMock.Verify(x => x.SetObjectJson($"event:{eventId1}", It.IsAny<GetEventByIdResponse>()), Times.Once);
        _cacheServiceMock.Verify(x => x.SetObjectJson($"event:{eventId2}", It.IsAny<GetEventByIdResponse>()), Times.Once);
    }
}