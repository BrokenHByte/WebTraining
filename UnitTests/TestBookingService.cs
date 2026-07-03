namespace Tests;

public class TestBookingService
{
    /*[Fact]
    public async Task TestBooking()
    {
        string dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var serviceProvider = services.BuildServiceProvider();


        var mockLogger1 = new Mock<ILogger<EventService>>();
        var mockLogger2 = new Mock<ILogger<BookingService>>();
        IEventService eventService = new EventService(serviceProvider.GetRequiredService<IEventRepository>());
        IBookingService bookingService = new BookingService(serviceProvider.GetRequiredService<IBookingRepository>(),
            serviceProvider.GetRequiredService<IEventService>(), mockLogger2.Object);

        List<Guid> guidsEvent = new();
        foreach (var ev in EventData.ExpectedTestData())
            guidsEvent.Add(await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 2));

        // Создаём невалидное
        var err = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await bookingService.CreateAsync(Guid.Empty));

        Assert.Equal("Event not found", err.Message);

        // Создаём несколько броней на одно событие
        await bookingService.CreateAsync(guidsEvent[0]);
        await bookingService.CreateAsync(guidsEvent[0]);
        var bookings = bookingService.GetAll().ToArray();
        Assert.Equal(2, bookings.Length);
        Assert.NotEqual(bookings[0].Id, bookings[1].Id);
        Assert.Equal(Booking.BookingStatus.Pending, bookings[0].Status);
        Assert.Equal(Booking.BookingStatus.Pending, bookings[1].Status);

        // Получаем Booking по Id
        var booking = await bookingService.GetByIdAsync(bookings[0].Id);
        Assert.Equal(booking.Id, bookings[0].Id);
        Assert.Equal(booking.EventId, bookings[0].EventId);
        Assert.Equal(booking.Status, bookings[0].Status);
        Assert.Equal(booking.CreatedAt, bookings[0].CreatedAt);
        Assert.Equal(booking.ProcessedAt, bookings[0].ProcessedAt);

        // Получаем бронь по невалидному ID
        var errBooking = await Assert.ThrowsAsync<BookingNotFoundException>(async () =>
            await bookingService.GetByIdAsync(Guid.Empty));

        Assert.Equal("Booking not found", errBooking.Message);

        // Проверка обновления c Confirm
        var processedAt = DateTime.UtcNow;
        var updatedBooking = (Booking)booking.Clone();
        updatedBooking.Confirm();
        await bookingService.Update(booking.Id, updatedBooking);
        booking = await bookingService.GetByIdAsync(bookings[0].Id);
        Assert.Equal(Booking.BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);

        // Проверка обновления c Reject
        updatedBooking = (Booking)booking.Clone();
        updatedBooking.Reject();
        await bookingService.Update(booking.Id, updatedBooking);
        booking = await bookingService.GetByIdAsync(bookings[0].Id);
        Assert.Equal(Booking.BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);

        // Проверка попытки добавления, когда мест нет
        var err2 = await Assert.ThrowsAsync<NoAvailableSeatsException>(async () =>
            await bookingService.CreateAsync(guidsEvent[0]));

        Assert.Equal("No available seats for this event", err2.Message);

        // Проверка что места освобождаются если удаляем бронь
        bookings = bookingService.GetAll().ToArray();
        var bookingGuid = bookings.Where(x => x.EventId == guidsEvent[0]).ToList();
        await bookingService.DeleteById(bookingGuid[0].Id);
        Event? eventTest = null;
        if (bookingGuid.Count > 0) eventTest = await eventService.GetEventByIdAsync(bookingGuid[0].EventId);

        if (eventTest != null)
        {
            Assert.Equal(1, eventTest.AvailableSeats);
            // И пробуем создать бронь вновь
            await bookingService.CreateAsync(guidsEvent[0]);
            Assert.Equal(0, eventTest.AvailableSeats);
        }


        // Создаём бронь на удалённое событие
        await eventService.DeleteEventByIdAsync(guidsEvent[0]);
        err = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await bookingService.CreateAsync(guidsEvent[0]));

        Assert.Equal("Event not found", err.Message);
    }


    [Fact]
    public async Task TestConcurrencyBookingBackgroundService()
    {
    }

    [Fact]
    public async Task TestBookingBackgroundService1()
    {
        // Проверим кейс
        //Дано: событие на 5 мест, 20 конкурентных запросов.
        //    Ожидается: ровно 5 успешных броней, 15 — NoAvailableSeatsException,
        string dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var serviceProvider = services.BuildServiceProvider();

        var mockLoggerEvent = new Mock<ILogger<EventService>>();
        var mockLoggerBooking = new Mock<ILogger<BookingService>>();
        IEventService eventService = new EventService(serviceProvider.GetRequiredService<IEventRepository>());
        await eventService.AddEventAsync("Test", "Test", DateTime.Now, DateTime.Now + new TimeSpan(1000), 5);

        var bookingService = new BookingService(serviceProvider.GetRequiredService<IBookingRepository>(), eventService,
            mockLoggerBooking.Object);

        var tasks = new List<Task>();
        var idEvent = (await eventService.GetEvents().ToListAsync())[0].Id;

        int countNoAvailableSeatsException = 0;

        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await bookingService.CreateAsync(idEvent);
                }
                catch (NoAvailableSeatsException ex)
                {
                    Interlocked.Increment(ref countNoAvailableSeatsException);
                }
            }));
        }

        await Task.WhenAll(tasks);
        Assert.Equal(15, countNoAvailableSeatsException);
        var bookings = bookingService.GetAll().ToList();
        Assert.Equal(5, bookings.Count);
        var eventResult = await eventService.GetEventByIdAsync(idEvent);
        Assert.Equal(0, eventResult.AvailableSeats);

        var mockLogger = new Mock<ILogger<BookingBackgroundService>>();
        var service = new BookingBackgroundService(serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            mockLogger.Object);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);

        // Проверим что все 5 броней, были переведены в Confirmed
        Assert.Equal(Booking.BookingStatus.Confirmed, bookings[0].Status);
    }

    [Fact]
    public async Task TestBookingBackgroundService2()
    {
        string dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        var serviceProvider = services.BuildServiceProvider();

        var mockLoggerEvent = new Mock<ILogger<EventService>>();
        var mockLoggerBooking = new Mock<ILogger<BookingService>>();
        IEventService eventService = new EventService(serviceProvider.GetRequiredService<IEventRepository>());
        await eventService.AddEventAsync("Test", "Test", DateTime.Now, DateTime.Now + new TimeSpan(1000), 10);

        var bookingService = new BookingService(serviceProvider.GetRequiredService<IBookingRepository>(), eventService,
            mockLoggerBooking.Object);

        var tasks = new List<Task>();
        var idEvent = (await eventService.GetEvents().ToListAsync())[0].Id;

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () => { await bookingService.CreateAsync(idEvent); }));
        }

        await Task.WhenAll(tasks);
        var bookings = bookingService.GetAll().ToList();
        Assert.Equal(bookings.Count(), bookings.Select(e => e.Id).Distinct().Count());
        Assert.Single(bookings.Select(e => e.EventId).Distinct());

        var mockLogger = new Mock<ILogger<BookingBackgroundService>>();
        BookingBackgroundService service = new(serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            mockLogger.Object);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(3000);
        await service.StopAsync(CancellationToken.None);
        // Проверим что все 10 броней, были переведены в Confirmed
        Assert.Equal(Booking.BookingStatus.Confirmed, bookings[0].Status);
    }*/
}