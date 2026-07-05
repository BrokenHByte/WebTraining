using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using WebProject.DataAccess;
using WebProject.Exceptions;
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
            "TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
    }


    public static TheoryData<EventTestData> CreateEventTestCases()
    {
        return new TheoryData<EventTestData>
        {
            // Первый набор данных
            new(
                new Event
                {
                    Title = "Test1",
                    Description = null,
                    StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(1),
                    TotalSeats = 10
                },
                false),
            new(
                new Event
                {
                    Title = "1",
                    Description = "2",
                    StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(1),
                    TotalSeats = 0
                },
                true,
                typeof(DbUpdateException)),
            new(
                new Event
                {
                    Title = null!,
                    Description = "2",
                    StartAt = DateTime.UtcNow,
                    EndAt = DateTime.UtcNow.AddHours(1),
                    TotalSeats = 3
                },
                true,
                typeof(DbUpdateException))
        };
    }

    [Theory]
    [MemberData(nameof(CreateEventTestCases))]
    public async Task CreateEventTest(EventTestData testData)
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);
        var data = testData.Input;

        if (testData is { ShouldThrowException: true, ExpectedExceptionType: not null })
        {
            await Assert.ThrowsAsync(testData.ExpectedExceptionType,
                async () => await repo.CreateAsync(data.Title, data.Description, data.StartAt, data.EndAt,
                    data.TotalSeats));
        }
        else
        {
            await repo.CreateAsync(data.Title, data.Description, data.StartAt, data.EndAt,
                data.TotalSeats);

            await using var contextControl = CreateContext();
            var repoControl = new EventRepository(mockLogger.Object, contextControl);
            var rows = await repoControl.GetWithFilter().ToListAsync();
            Assert.Single(rows);
            Assert.Equal(data.Title, rows.First().Title);
            Assert.Equal(data.Description, rows.First().Description);
            Assert.Equal(data.StartAt, rows.First().StartAt, TimeSpan.FromMilliseconds(1));
            Assert.Equal(data.EndAt, rows.First().EndAt, TimeSpan.FromMilliseconds(1));
            Assert.Equal(data.TotalSeats, rows.First().TotalSeats);
        }
    }

    [Fact]
    public async Task UpdateEventTest()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);
        var guid = await repo.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            10);

        var newData = new Event
        {
            Title = "Fun",
            Description = "Fun",
            StartAt = DateTime.UtcNow,
            EndAt = DateTime.UtcNow.AddDays(1),
            TotalSeats = 10
        };

        await using var contextControl = CreateContext();
        var repoControl = new EventRepository(mockLogger.Object, contextControl);
        await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await repoControl.UpdateAsync(Guid.NewGuid(), newData));

        await using var contextControl2 = CreateContext();
        var repoControl2 = new EventRepository(mockLogger.Object, contextControl2);
        await repoControl2.UpdateAsync(guid, newData);

        await using var contextControl3 = CreateContext();
        var repoControl3 = new EventRepository(mockLogger.Object, contextControl3);
        var updateEvent = await repoControl2.GetByIdAsync(guid);

        Assert.Equal(newData.Title, updateEvent.Title);
        Assert.Equal(newData.Description, updateEvent.Description);
        Assert.Equal(newData.StartAt, updateEvent.StartAt);
        Assert.Equal(newData.EndAt, updateEvent.EndAt);
        Assert.Equal(newData.TotalSeats, newData.TotalSeats);
    }

    [Fact]
    public async Task DeleteEventTest()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);
        var guid = await repo.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            10);

        await using var contextControl = CreateContext();
        var repoControl = new EventRepository(mockLogger.Object, contextControl);
        await repoControl.DeleteByIdAsync(guid);

        await using var contextControl2 = CreateContext();
        var repoControl2 = new EventRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<EventNotFoundException>(async () => await repoControl2.DeleteByIdAsync(guid));
    }

    [Fact]
    public async Task GetEventByIdAsyncTest()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);
        var guid = await repo.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            10);

        await using var contextControl = CreateContext();
        var repoControl = new EventRepository(mockLogger.Object, contextControl);
        var updateEvent = await repoControl.GetByIdAsync(guid);

        await using var contextControl2 = CreateContext();
        var repoControl2 = new EventRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await repoControl2.DeleteByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ContainsByIdAsyncTest()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);
        var guid = await repo.CreateAsync("Test", "Test", DateTime.UtcNow, DateTime.UtcNow.AddDays(1),
            10);

        await using var contextControl = CreateContext();
        var repoControl = new EventRepository(mockLogger.Object, contextControl);
        Assert.True(await repoControl.ContainsByIdAsync(guid));

        await using var contextControl2 = CreateContext();
        var repoControl2 = new EventRepository(mockLogger.Object, contextControl2);
        Assert.False(await repoControl2.ContainsByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetEventsTest()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);

        for (int i = 0; i < 20; i++)
        {
            await repo.CreateAsync("Test_" + i, "Test" + i, DateTime.UtcNow.AddDays(i).AddHours(1),
                DateTime.UtcNow.AddDays(i + 1).AddHours(1),
                10);
        }

        await using var context2 = CreateContext();
        var repo2 = new EventRepository(mockLogger.Object, context2);
        var rows = await repo2.GetWithFilter().ToListAsync();
        Assert.Equal(20, rows.Count);

        var rowsTitle = await repo2.GetWithFilter("st_1").ToListAsync();
        Assert.Equal(11, rowsTitle.Count);

        var rowsTitleEmpty = await repo2.GetWithFilter("").ToListAsync();
        Assert.Equal(20, rowsTitleEmpty.Count);

        var rowsRange1 = await repo2.GetWithFilter(null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(10))
            .ToListAsync();

        Assert.Equal(9, rowsRange1.Count);

        var rowsRange2 = await repo2.GetWithFilter(null, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(30))
            .ToListAsync();

        Assert.Equal(10, rowsRange2.Count);

        var rowsMix = await repo2.GetWithFilter("st_1", DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(30))
            .ToListAsync();

        Assert.Equal(10, rowsMix.Count);
    }

    [Fact]
    public async Task PaginationTest()
    {
        await ResetDatabaseAsync();
        await using var context = CreateContext();
        var mockLogger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(mockLogger.Object, context);

        for (int i = 0; i < 20; i++)
        {
            await repo.CreateAsync("Test_" + i, "Test" + i, DateTime.UtcNow.AddDays(i).AddHours(1),
                DateTime.UtcNow.AddDays(i + 1).AddHours(1),
                10);
        }

        await using var context2 = CreateContext();
        var repo2 = new EventRepository(mockLogger.Object, context2);
        var query = repo2.GetWithFilter();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await repo2.Pagination(query, 0, 0).ToListAsync());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await repo2.Pagination(query, 0, 10).ToListAsync());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await repo2.Pagination(query, -1, 10).ToListAsync());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await repo2.Pagination(query, 1, 0).ToListAsync());

        var rowPagination = await repo2.Pagination(query, 1, 5).ToListAsync();
        Assert.Equal(5, rowPagination.Count);
        Assert.Equal("Test_0", rowPagination[0].Title);

        var rowPagination2 = await repo2.Pagination(query, 2, 5).ToListAsync();
        Assert.Equal(5, rowPagination2.Count);
        Assert.Equal("Test_5", rowPagination2[0].Title);

        var rowPagination3 = await repo2.Pagination(query, 5, 5).ToListAsync();
        Assert.Empty(rowPagination3);
    }
}