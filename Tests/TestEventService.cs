using Microsoft.Extensions.Logging;
using Moq;
using WebProject.Exceptions;
using WebProject.Services;

namespace Tests;

public class TestEventService
{
    /*
     * создание события                         - TestAddEvent;
     * получение всех событий                   - TestGetAllEvent;
     * обновление существующего события;        - TestUpdateEvent
     * удаление существующего события           - TestDeleteEvent;
     * фильтрация по названию                   - TestGetAllEvent;
     * фильтрация по датам (startDate, endDate) - TestGetAllEvent;
     * пагинация событий;                       - TestPageEvent
     * комбинированная фильтрация               - TestGetAllEvent;
     *
     * попытка получить событие с несуществующим ID                        - TestGetEventByIndex;
     * попытка обновить событие с несуществующим ID                        - TestUpdateEvent;
     * создание события с некорректными данными (если валидация в сервисе) - TestAddEvent;
     * обновление события с некорректными датами (EndAt раньше StartAt).   - TestUpdateEvent;
     */


    [Fact]
    public async Task TestAddEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);

        foreach (var ev in EventData.AddTestData())
        {
            var dataEvent = ev.Item1;

            if (ev.Item2.Length > 0)
            {
                var err = await Assert.ThrowsAsync<EventValidationException>(async () =>
                    await eventService.AddEventAsync(dataEvent.Title, dataEvent.Description, dataEvent.StartAt,
                        dataEvent.EndAt, 10));

                Assert.Equal(err.Message, ev.Item2);
            }
            else
            {
                await eventService.AddEventAsync(dataEvent.Title, dataEvent.Description, dataEvent.StartAt,
                    dataEvent.EndAt, 10);
            }
        }
    }

    [Fact]
    public async Task TestDeleteEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        var addData = EventData.ExpectedTestData().ToArray();
        List<Guid> eventIds = new();
        foreach (var ev in addData)
            eventIds.Add(await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 10));

        // Ожидаем существование четырёх записей [0-3]
        await eventService.DeleteEventByIdAsync(eventIds[0]);
        var err = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await eventService.DeleteEventByIdAsync(eventIds[0]));

        Assert.Equal("Event not found", err.Message);
        await eventService.DeleteEventByIdAsync(eventIds[1]);
        await eventService.DeleteEventByIdAsync(eventIds[2]);
        var all = (await eventService.GetEventsAsync()).ToList();
        Assert.Equal(all.Count, EventData.ExpectedTestData().Count() - 3);
    }

    [Fact]
    public async Task TestGetEventByIndex()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        List<Guid> eventIds = new();
        foreach (var ev in EventData.ExpectedTestData())
            eventIds.Add(await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 10));

        var err = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await eventService.GetEventByIdAsync(Guid.Empty));

        Assert.Equal("Event not found", err.Message);

        var firstEvent = await eventService.GetEventByIdAsync(eventIds.First());
        Assert.NotNull(firstEvent);
        var lastEvent = await eventService.GetEventByIdAsync(eventIds.Last());
        Assert.Equal(EventData.ExpectedTestData().Last().Title, lastEvent.Title);
    }

    [Fact]
    public async Task TestGetAllEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.ExpectedTestData())
            await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 10);

        // Ожидаем существование четырёх записей [0-3]
        var events = (await eventService.GetEventsAsync()).ToList();
        Assert.Equal(EventData.ExpectedTestData().Count(), events.Count);
    }

    [Fact]
    public async Task TestGetAllWithFilterEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.ExpectedTestData())
            await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 10);

        // -------- Заголовки -------
        // Все по началу строки заголовка
        int targetCount = EventData.ExpectedTestData().Count();
        var events = (await eventService.GetEventsAsync("")).ToList();
        Assert.Equal(targetCount, events.Count);
        events = (await eventService.GetEventsAsync("Tit")).ToList();
        Assert.Equal(targetCount - 1, events.Count);

        // Конкретное значение
        events = (await eventService.GetEventsAsync("Title2")).ToList();
        Assert.Single(events);

        // Не существующее значение заголовка
        events = (await eventService.GetEventsAsync("Title1")).ToList();
        Assert.Empty(events);

        // -------- Даты -------
        // Все добавленные   
        events = (await eventService.GetEventsAsync(null, EventData.dateExample)).ToList();
        Assert.Equal(targetCount, events.Count);
        events = (await eventService.GetEventsAsync(null, EventData.dateExample - EventData.OffsetShort)).ToList();
        Assert.Equal(targetCount, events.Count);
        // Одно событие, Test 6
        events = (await eventService.GetEventsAsync(null, EventData.dateExample + EventData.OffsetShort)).ToList();
        Assert.Single(events);
        // Никаких событий для поздней даты
        events = (await eventService.GetEventsAsync(null, EventData.dateExample + EventData.OffsetLong)).ToList();
        Assert.Empty(events);

        events = (await eventService.GetEventsAsync(null, null, EventData.dateExample + EventData.OffsetLong)).ToList();
        Assert.Equal(targetCount, events.Count);
        events = (await eventService.GetEventsAsync(null, null, EventData.dateExample + EventData.OffsetShort))
            .ToList();

        Assert.Equal(2, events.Count);
        events = (await eventService.GetEventsAsync(null, null, EventData.dateExample)).ToList();
        Assert.Empty(events);
        events = (await eventService.GetEventsAsync(null, null, EventData.dateExample - EventData.OffsetShort))
            .ToList();

        Assert.Empty(events);

        // Смешаные кейсы
        events = (await eventService.GetEventsAsync("Tit", EventData.dateExample)).ToList();
        Assert.Equal(targetCount - 1, events.Count);
        events = (await eventService.GetEventsAsync("Tit", EventData.dateExample + EventData.OffsetLong)).ToList();
        Assert.Empty(events);
        events = (await eventService.GetEventsAsync("Tit", null, EventData.dateExample - EventData.OffsetLong))
            .ToList();

        Assert.Empty(events);
        events = (await eventService.GetEventsAsync("", EventData.dateExample,
                EventData.dateExample + EventData.OffsetLong))
            .ToList();

        Assert.Equal(targetCount, events.Count);
    }

    [Fact]
    public async Task TestUpdateEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        List<Guid> eventIds = new();
        foreach (var ev in EventData.ExpectedTestData())
            eventIds.Add(await eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 10));

        var evs = EventData.UpdateTestData().ToList();
        await eventService.UpdateEventAsync(eventIds[0], evs[0].Item1);
        var err = await Assert.ThrowsAsync<EventValidationException>(() =>
            eventService.UpdateEventAsync(eventIds[1], evs[1].Item1));

        Assert.Equal(err.Message, EventData.messageInvalid);

        await eventService.UpdateEventAsync(eventIds[2], evs[2].Item1);
        var err2 = await Assert.ThrowsAsync<EventNotFoundException>(async () =>
            await eventService.UpdateEventAsync(Guid.Empty, evs[3].Item1));

        Assert.Equal("Event not found", err2.Message);
    }

    [Fact]
    public async Task TestPageEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.ExpectedPageTestData())
            eventService.AddEventAsync(ev.Title, ev.Description, ev.StartAt, ev.EndAt, 10);

        var events = (await eventService.GetEventsAsync()).ToList();
        const int sizePage = 30;
        var page1 = (await eventService.GetPageAsync(events, 1, sizePage)).ToList();
        var page2 = (await eventService.GetPageAsync(events, 2, sizePage)).ToList();
        var page3 = (await eventService.GetPageAsync(events, 3, sizePage)).ToList();
        var page4 = (await eventService.GetPageAsync(events, 4, sizePage)).ToList();

        Assert.Equal(30, page1.Count);
        Assert.Equal(30, page2.Count);
        Assert.Equal(30, page3.Count);
        Assert.Equal(10, page4.Count);

        Assert.Equal(page1[0].Title, events[0].Title);
        Assert.Equal(page4[9].Title, events.Last().Title);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await eventService.GetPageAsync(events, 0, sizePage));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await eventService.GetPageAsync(events, 1, 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await eventService.GetPageAsync(events, -100, sizePage));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await eventService.GetPageAsync(events, 1, -100));
    }
}