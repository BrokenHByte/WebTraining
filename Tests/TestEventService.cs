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
    public void TestAddEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.AddTestData())
        {
            var dataEvent = ev.Item1;
            if (ev.Item2.Length > 0)
            {
                var err = Assert.Throws<EventValidationException>(() =>
                    eventService.AddEvent(dataEvent.Title, dataEvent.Description, dataEvent.StartAt, dataEvent.EndAt));
                Assert.Equal(err.Message, ev.Item2);
            }
            else
            {
                eventService.AddEvent(dataEvent.Title, dataEvent.Description, dataEvent.StartAt, dataEvent.EndAt);
            }
        }
    }

    [Fact]
    public void TestDeleteEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        var addData = EventData.ExpectedTestData().ToArray();
        List<Guid> eventIds = new();
        foreach (var ev in addData) eventIds.Add(eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt));

        // Ожидаем существование четырёх записей [0-3]
        eventService.DeleteEventById(eventIds[0]);
        var err = Assert.Throws<EventNotFoundException>(() => eventService.DeleteEventById(eventIds[0]));
        Assert.Equal("Event not found", err.Message);
        eventService.DeleteEventById(eventIds[1]);
        eventService.DeleteEventById(eventIds[2]);
        var all = eventService.GetEvents().ToList();
        Assert.Equal(all.Count, EventData.ExpectedTestData().Count() - 3);
    }

    [Fact]
    public void TestGetEventByIndex()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        List<Guid> eventIds = new();
        foreach (var ev in EventData.ExpectedTestData())
            eventIds.Add(eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt));

        var err = Assert.Throws<EventNotFoundException>(() => eventService.GetEventById(Guid.Empty));
        Assert.Equal("Event not found", err.Message);

        var firstEvent = eventService.GetEventById(eventIds.First());
        Assert.NotNull(firstEvent);
        var lastEvent = eventService.GetEventById(eventIds.Last());
        Assert.Equal(EventData.ExpectedTestData().Last().Title, lastEvent.Title);
    }

    [Fact]
    public void TestGetAllEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.ExpectedTestData())
            eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt);

        // Ожидаем существование четырёх записей [0-3]
        var events = eventService.GetEvents().ToList();
        Assert.Equal(EventData.ExpectedTestData().Count(), events.Count);
    }

    [Fact]
    public void TestGetAllWithFilterEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.ExpectedTestData())
            eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt);

        // -------- Заголовки -------
        // Все по началу строки заголовка
        var targetCount = EventData.ExpectedTestData().Count();
        var events = eventService.GetEvents("").ToList();
        Assert.Equal(targetCount, events.Count);
        events = eventService.GetEvents("Tit").ToList();
        Assert.Equal(targetCount - 1, events.Count);

        // Конкретное значение
        events = eventService.GetEvents("Title2").ToList();
        Assert.Single(events);

        // Не существующее значение заголовка
        events = eventService.GetEvents("Title1").ToList();
        Assert.Empty(events);

        // -------- Даты -------
        // Все добавленные   
        events = eventService.GetEvents(null, EventData.dateExample).ToList();
        Assert.Equal(targetCount, events.Count);
        events = eventService.GetEvents(null, EventData.dateExample - EventData.OffsetShort).ToList();
        Assert.Equal(targetCount, events.Count);
        // Одно событие, Test 6
        events = eventService.GetEvents(null, EventData.dateExample + EventData.OffsetShort).ToList();
        Assert.Single(events);
        // Никаких событий для поздней даты
        events = eventService.GetEvents(null, EventData.dateExample + EventData.OffsetLong).ToList();
        Assert.Empty(events);

        events = eventService.GetEvents(null, null, EventData.dateExample + EventData.OffsetLong).ToList();
        Assert.Equal(targetCount, events.Count);
        events = eventService.GetEvents(null, null, EventData.dateExample + EventData.OffsetShort).ToList();
        Assert.Equal(2, events.Count);
        events = eventService.GetEvents(null, null, EventData.dateExample).ToList();
        Assert.Empty(events);
        events = eventService.GetEvents(null, null, EventData.dateExample - EventData.OffsetShort).ToList();
        Assert.Empty(events);

        // Смешаные кейсы
        events = eventService.GetEvents("Tit", EventData.dateExample).ToList();
        Assert.Equal(targetCount - 1, events.Count);
        events = eventService.GetEvents("Tit", EventData.dateExample + EventData.OffsetLong).ToList();
        Assert.Empty(events);
        events = eventService.GetEvents("Tit", null, EventData.dateExample - EventData.OffsetLong).ToList();
        Assert.Empty(events);
        events = eventService.GetEvents("", EventData.dateExample, EventData.dateExample + EventData.OffsetLong)
            .ToList();
        Assert.Equal(targetCount, events.Count);
    }

    [Fact]
    public void TestUpdateEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        List<Guid> eventIds = new();
        foreach (var ev in EventData.ExpectedTestData())
            eventIds.Add(eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt));

        var evs = EventData.UpdateTestData().ToList();
        eventService.UpdateEvent(eventIds[0], evs[0].Item1);
        var err = Assert.Throws<EventValidationException>(() =>
            eventService.UpdateEvent(eventIds[1], evs[1].Item1));
        Assert.Equal(err.Message, EventData.messageInvalid);
        eventService.UpdateEvent(eventIds[2], evs[2].Item1);
        var err2 = Assert.Throws<EventNotFoundException>(() => eventService.UpdateEvent(Guid.Empty, evs[3].Item1));
        Assert.Equal("Event not found", err2.Message);
    }

    [Fact]
    public void TestPageEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in EventData.ExpectedPageTestData())
            eventService.AddEvent(ev.Title, ev.Description, ev.StartAt, ev.EndAt);

        var events = eventService.GetEvents().ToList();
        const int sizePage = 30;
        var page1 = eventService.GetPage(events, 1, sizePage).ToList();
        var page2 = eventService.GetPage(events, 2, sizePage).ToList();
        var page3 = eventService.GetPage(events, 3, sizePage).ToList();
        var page4 = eventService.GetPage(events, 4, sizePage).ToList();

        Assert.Equal(30, page1.Count);
        Assert.Equal(30, page2.Count);
        Assert.Equal(30, page3.Count);
        Assert.Equal(10, page4.Count);

        Assert.Equal(page1[0].Title, events[0].Title);
        Assert.Equal(page4[9].Title, events.Last().Title);

        Assert.Throws<ArgumentOutOfRangeException>(() => eventService.GetPage(events, 0, sizePage));
        Assert.Throws<ArgumentOutOfRangeException>(() => eventService.GetPage(events, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => eventService.GetPage(events, -100, sizePage));
        Assert.Throws<ArgumentOutOfRangeException>(() => eventService.GetPage(events, 1, -100));
    }
}