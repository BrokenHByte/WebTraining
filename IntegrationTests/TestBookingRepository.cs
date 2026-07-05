using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using WebProject.DataAccess;
using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Repositories;

namespace IntegrationTests;

public class TestBookingRepository : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = createContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    private AppDbContext createContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }

    private async Task resetDatabaseAsync()
    {
        await using var context = createContext();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE bookings, events RESTART IDENTITY CASCADE");
    }

    private async Task<List<Guid>> prepareDataEvent()
    {
        List<Guid> guids = [];
        await using var context = createContext();
        var mockEventLogger = new Mock<ILogger<EventRepository>>();
        var repository = new EventRepository(mockEventLogger.Object, context);

        for (int i = 0; i < 3; i++)
        {
            guids.Add(await repository.CreateAsync("Test_" + i, "Test" + i, DateTime.UtcNow.AddDays(i).AddHours(1),
                DateTime.UtcNow.AddDays(i + 1).AddHours(1),
                2));
        }

        return guids;
    }

    [Fact]
    public async Task CreateBookingTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await repo.CreateAsync(Guid.NewGuid()));

        context.ChangeTracker.Clear();

        var booking = await repo.CreateAsync(guids[0]);

        await using var contextControl = createContext();
        var repo2 = new BookingRepository(mockLogger.Object, contextControl);
        var bookingControl = await repo2.GetByIdAsync(booking.Id);
    }

    [Fact]
    public async Task UpdateBookingTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0]);

        var d = DateTime.UtcNow.AddDays(1);
        await using var contextUpdate = createContext();
        var repo2 = new BookingRepository(mockLogger.Object, contextUpdate);
        var dataToBooking = new Booking
        {
            Status = Booking.BookingStatus.Confirmed,
            ProcessedAt = d
        };

        await repo2.UpdateAsync(booking.Id, dataToBooking);

        await using var contextControl = createContext();
        var repo3 = new BookingRepository(mockLogger.Object, contextControl);
        var updateBooking = await repo3.GetByIdAsync(booking.Id);
        Assert.Equal(updateBooking.Status, dataToBooking.Status);
        Assert.NotNull(updateBooking.ProcessedAt);
        Assert.Equal(updateBooking.ProcessedAt.Value, dataToBooking.ProcessedAt.Value, TimeSpan.FromMilliseconds(1));

        await using var contextControl2 = createContext();
        var repo4 = new BookingRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            await repo4.UpdateAsync(Guid.NewGuid(), dataToBooking));
    }

    [Fact]
    public async Task DeleteBookingTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0]);

        await using var contextControl = createContext();
        var repo2 = new BookingRepository(mockLogger.Object, contextControl);
        await repo2.DeleteByIdAsync(booking.Id);

        await using var contextControl2 = createContext();
        var repo3 = new BookingRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            await repo3.DeleteByIdAsync(booking.Id));
    }

    [Fact]
    public async Task GetBookingByIdAsyncTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0]);

        await using var contextControl = createContext();
        var repoControl = new BookingRepository(mockLogger.Object, contextControl);
        var resBooking = await repoControl.GetByIdAsync(booking.Id);
        Assert.Equal(resBooking.Status, booking.Status);
        Assert.Equal(resBooking.CreatedAt, booking.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.Null(resBooking.ProcessedAt);

        await using var contextControl2 = createContext();
        var repoControl2 = new BookingRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<BookingNotFoundException>(async () => await repoControl.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetBookingByEventAsyncTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0]);

        await using var contextControl = createContext();
        var repoControl = new BookingRepository(mockLogger.Object, contextControl);
        var resBooking = await repoControl.GetBookingsByEvent(guids[0]).ToListAsync();
        Assert.Single(resBooking);
        Assert.Equal(resBooking[0].CreatedAt, booking.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(resBooking[0].EventId, booking.EventId);
        Assert.Equal(resBooking[0].Status, booking.Status);

        await using var contextControl2 = createContext();
        var repoControl2 = new BookingRepository(mockLogger.Object, contextControl2);
        var resBooking2 = await repoControl2.GetBookingsByEvent(Guid.NewGuid()).ToListAsync();
        Assert.Empty(resBooking2);
    }

    [Fact]
    public async Task GetAllTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0]);
        var booking2 = await repo.CreateAsync(guids[1]);

        await using var context2 = createContext();
        var repo2 = new BookingRepository(mockLogger.Object, context2);
        var pendingBookings = await repo2.GetAll().ToListAsync();
        Assert.Equal(2, pendingBookings.Count);
    }

    [Fact]
    public async Task GetPendingTest()
    {
        await resetDatabaseAsync();
        var guids = await prepareDataEvent();

        await using var context = createContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0]);
        var booking2 = await repo.CreateAsync(guids[1]);

        await using var context2 = createContext();
        var repo2 = new BookingRepository(mockLogger.Object, context2);
        var pendingBookings = await repo2.GetPending().ToListAsync();
        Assert.Equal(2, pendingBookings.Count);
        Assert.Equal(Booking.BookingStatus.Pending, pendingBookings[0].Status);
        Assert.Equal(Booking.BookingStatus.Pending, pendingBookings[1].Status);
    }
}