using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Presentation.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;

public class EventServiceTests
{
    private readonly Mock<ILogger<EventService>> _mockLogger = new();
    private readonly Mock<IEventRepository> _mockRepository = new();

    [Fact]
    public async Task CreateAsync()
    {
        var service = new EventService(_mockRepository.Object, _mockLogger.Object);
        await service.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await Assert.ThrowsAsync<EventValidationException>(async () => await
            service.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddHours(-1), 10));
    }

    [Fact]
    public async Task UpdateAsync()
    {
        var service = new EventService(_mockRepository.Object, _mockLogger.Object);
        var guid = await service.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
        await Assert.ThrowsAsync<EventValidationException>(async () => await
            service.UpdateAsync(guid,
                new Event
                {
                    Title = "Test", Description = "Test", StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(-1), AvailableSeats = 10
                }));
    }
}