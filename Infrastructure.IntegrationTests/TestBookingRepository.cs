using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;


namespace IntegrationTests;

[Collection("Postgres Collection")]
public class TestBookingRepository(PostgresFixture fixture)
{
    private async Task<(List<Guid>, Guid)> prepareDataEvent()
    {
        List<Guid> guids = [];
        await using var context = fixture.CreateContext();
        var mockEventLogger = new Mock<ILogger<EventRepository>>();
        var repository = new EventRepository(mockEventLogger.Object, context);

        for (int i = 0; i < 3; i++)
        {
            guids.Add(await repository.CreateAsync("Test_" + i, "Test" + i, DateTime.UtcNow.AddDays(i).AddHours(1),
                DateTime.UtcNow.AddDays(i + 1).AddHours(1),
                2));
        }

        var userId = Guid.NewGuid();
        context.Users.Add(new User()
        {
            Id = userId,
            Login = "TestLogin",
            HashPass = "123",
            Role = User.Roles.Admin
        });
        await context.SaveChangesAsync();
        return (guids, userId);
    }

    [Fact]
    public async Task CreateBookingTest()
    {
        await fixture.ResetDatabaseAsync();
        
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await repo.CreateAsync(Guid.NewGuid(), userId));

        context.ChangeTracker.Clear();

        var booking = await repo.CreateAsync(guids[0], userId);

        await using var contextControl = fixture.CreateContext();
        var repo2 = new BookingRepository(mockLogger.Object, contextControl);
        var bookingControl = await repo2.GetByIdAsync(booking.Id);
    }

    [Fact]
    public async Task UpdateBookingTest()
    {
        await fixture.ResetDatabaseAsync();
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0], userId);

        var d = DateTime.UtcNow.AddDays(1);
        await using var contextUpdate = fixture.CreateContext();
        var repo2 = new BookingRepository(mockLogger.Object, contextUpdate);
        var dataToBooking = new Booking
        {
            Status = Booking.BookingStatus.Confirmed,
            ProcessedAt = d
        };

        await repo2.UpdateAsync(booking.Id, dataToBooking);

        await using var contextControl = fixture.CreateContext();
        var repo3 = new BookingRepository(mockLogger.Object, contextControl);
        var updateBooking = await repo3.GetByIdAsync(booking.Id);
        Assert.Equal(updateBooking.Status, dataToBooking.Status);
        Assert.NotNull(updateBooking.ProcessedAt);
        Assert.Equal(updateBooking.ProcessedAt.Value, dataToBooking.ProcessedAt.Value, TimeSpan.FromMilliseconds(1));

        await using var contextControl2 = fixture.CreateContext();
        var repo4 = new BookingRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            await repo4.UpdateAsync(Guid.NewGuid(), dataToBooking));
    }

    [Fact]
    public async Task DeleteBookingTest()
    {
        await fixture.ResetDatabaseAsync();
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0], userId);

        await using var contextControl = fixture.CreateContext();
        var repo2 = new BookingRepository(mockLogger.Object, contextControl);
        await repo2.DeleteByIdAsync(booking.Id);

        await using var contextControl2 = fixture.CreateContext();
        var repo3 = new BookingRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            await repo3.DeleteByIdAsync(booking.Id));
    }

    [Fact]
    public async Task GetBookingByIdAsyncTest()
    {
        await fixture.ResetDatabaseAsync();
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0], userId);

        await using var contextControl = fixture.CreateContext();
        var repoControl = new BookingRepository(mockLogger.Object, contextControl);
        var resBooking = await repoControl.GetByIdAsync(booking.Id);
        Assert.Equal(resBooking.Status, booking.Status);
        Assert.Equal(resBooking.CreatedAt, booking.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.Null(resBooking.ProcessedAt);

        await using var contextControl2 = fixture.CreateContext();
        var repoControl2 = new BookingRepository(mockLogger.Object, contextControl2);
        await Assert.ThrowsAsync<BookingNotFoundException>(async () => await repoControl.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetBookingByEventAsyncTest()
    {
        await fixture.ResetDatabaseAsync();
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0], userId);

        await using var contextControl = fixture.CreateContext();
        var repoControl = new BookingRepository(mockLogger.Object, contextControl);
        var resBooking = await repoControl.GetBookingsByEvent(guids[0]).ToListAsync();
        Assert.Single(resBooking);
        Assert.Equal(resBooking[0].CreatedAt, booking.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(resBooking[0].EventId, booking.EventId);
        Assert.Equal(resBooking[0].Status, booking.Status);

        await using var contextControl2 = fixture.CreateContext();
        var repoControl2 = new BookingRepository(mockLogger.Object, contextControl2);
        var resBooking2 = await repoControl2.GetBookingsByEvent(Guid.NewGuid()).ToListAsync();
        Assert.Empty(resBooking2);
    }

    [Fact]
    public async Task GetAllTest()
    {
        await fixture.ResetDatabaseAsync();
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0], userId);
        var booking2 = await repo.CreateAsync(guids[1], userId);

        await using var context2 = fixture.CreateContext();
        var repo2 = new BookingRepository(mockLogger.Object, context2);
        var pendingBookings = await repo2.GetAll().ToListAsync();
        Assert.Equal(2, pendingBookings.Count);
    }

    [Fact]
    public async Task GetPendingTest()
    {
        await fixture.ResetDatabaseAsync();
        var data = await prepareDataEvent();
        var guids = data.Item1;
        var userId = data.Item2;

        await using var context = fixture.CreateContext();
        var mockLogger = new Mock<ILogger<BookingRepository>>();
        var repo = new BookingRepository(mockLogger.Object, context);

        var booking = await repo.CreateAsync(guids[0], userId);
        var booking2 = await repo.CreateAsync(guids[1], userId);

        await using var context2 = fixture.CreateContext();
        var repo2 = new BookingRepository(mockLogger.Object, context2);
        var pendingBookings = await repo2.GetPending().ToListAsync();
        Assert.Equal(2, pendingBookings.Count);
        Assert.Equal(Booking.BookingStatus.Pending, pendingBookings[0].Status);
        Assert.Equal(Booking.BookingStatus.Pending, pendingBookings[1].Status);
    }
}