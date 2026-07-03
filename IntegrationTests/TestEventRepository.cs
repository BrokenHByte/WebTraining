using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using WebProject.DataAccess;
using WebProject.Models;
using WebProject.Repositories;

namespace IntegrationTests;

public record EventTestData(
    Event Input,
    bool ShouldThrowException,
    Type? ExpectedExceptionType = null
);

public class TestEventRepository : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        await context.DisposeAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    private async Task ResetDatabaseAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE events, bookings RESTART IDENTITY CASCADE");
    }


    public static TheoryData<EventTestData> AddEventTestCases()
    {
        return new TheoryData<EventTestData>
        {
            // Первый набор данных
            new(
                new Event
                {
                    Title = "Test1",
                    Description = "Desc",
                    StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(1),
                    TotalSeats = 10
                },
                false),
            new(
                new Event
                {
                    Title = "",
                    Description = "",
                    StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(-1),
                    TotalSeats = -1
                },
                false) // Логика валидации вынесена в сервис
        };
    }

    /// <summary>
    ///     Тестируем создание событий в базе postgres
    /// </summary>
    [Theory]
    [MemberData(nameof(AddEventTestCases))]
    public async Task CreateEvent(EventTestData testData)
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);
        var data = testData.Input;
        if (testData.ShouldThrowException)
            await Assert.ThrowsAsync(testData.ExpectedExceptionType,
                async () => await repo.AddEventAsync(data.Title, data.Description, data.StartAt, data.EndAt,
                    data.TotalSeats));
        else
            await repo.AddEventAsync(data.Title, data.Description, data.StartAt, data.EndAt,
                data.TotalSeats);
    }
}