using Microsoft.Extensions.Logging;
using Moq;
using WebProject.Exceptions;
using WebProject.Models;
using WebProject.Services;

namespace Tests;

public class TestEventService
{
    private readonly DateTime _date = new(1989, 10, 07);
    private readonly TimeSpan _offsetShort = TimeSpan.FromHours(1);
    private readonly TimeSpan _offsetLong = TimeSpan.FromHours(2);
    private readonly string messageInvalid = "Event with id is invalid: EndAt <= StartAt";
    
    // Данные для проверки вставки
    IEnumerable<(Event, string)> AddTestData()
    {
        return
        [
            (new Event { Title = "Title1", Description = "Test1", StartAt = _date + _offsetShort, EndAt = _date }, messageInvalid),
            (new Event { Title = "Title2", Description = "Test2", StartAt = _date, EndAt = _date + _offsetShort }, ""),
            (new Event { Title = "Title3", Description = null, StartAt = _date, EndAt = _date }, messageInvalid),
            (new Event { Title = "Title4", Description = "Test4", StartAt = _date, EndAt = _date + _offsetShort }, ""),
            (new Event { Title = "", Description = "Test5", StartAt = _date, EndAt = _date + _offsetLong }, ""),
            (new Event { Title = "Title6", Description = "Test6", StartAt = _date + _offsetShort, EndAt = _date + _offsetLong }, ""),
        ];
    }

    // Произвольный набор валидных данных
    IEnumerable<Event> ExpectedTestData()
    {
        return
        [
            new Event { Title = "Title2", Description = "Test2", StartAt = _date, EndAt = _date + _offsetShort },
            new Event { Title = "Title4", Description = "Test4", StartAt = _date, EndAt = _date + _offsetShort },
            new Event { Title = "", Description = "Test5", StartAt = _date, EndAt = _date + _offsetLong },
            new Event
            {
                Title = "Title6", Description = "Test6", StartAt = _date + _offsetShort, EndAt = _date + _offsetLong
            }
        ];
    }

    // Обновление для ExpectedTestData набора
    IEnumerable<(Event, string)> UpdateTestData()
    {
        return
        [
            (new Event { Id = Guid.NewGuid(), Title = "NewTitle2", Description = "Test2", StartAt = _date, EndAt = _date + _offsetShort }, ""),
            (new Event { Id = Guid.NewGuid(), Title = "NewTitle4", Description = "Test4", StartAt = _date + _offsetShort, EndAt = _date }, messageInvalid),
            (new Event { Id = Guid.NewGuid(), Title = "", Description = null, StartAt = _date, EndAt = _date + _offsetLong }, ""),
            (new Event { Id = Guid.NewGuid(), Title = "NewTitle6", Description = "Test6", StartAt = _date + _offsetShort, EndAt = _date + _offsetLong }, "Event 100 not found"),
        ];
    }

    static IEnumerable<(int, string)> TestDeleteData()
    {
        return
        [
            (1, ""),
            (2, ""),
            (2, "Event 2 not found"),
            (3, "")
        ];
    }

    // Набор для теста постраничного получения данных
    IEnumerable<Event> ExpectedPageTestData()
    {
        for (int i = 0; i < 100; i++)
        {
            yield return new Event()
            {
                Title = $"Title{i + 1}", Description = $"Test{i + 1}", StartAt = _date, EndAt = _date + _offsetShort
            };
        }
    }

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
        foreach (var ev in AddTestData())
        {
            if (ev.Item2.Length > 0)
            {
                var err = Assert.Throws<EventValidationException>(() => eventService.AddEvent(ev.Item1));
                Assert.Equal(err.Message, ev.Item2);
            }
            else
            {
                eventService.AddEvent(ev.Item1);
            }
        }
    }

    [Fact]
    public void TestDeleteEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        // Ожидаем существование четырёх записей [0-3]
    //   eventService.DeleteEventById(0);
     //   var err = Assert.Throws<EventNotFoundException>(() => eventService.DeleteEventById(0));
    //    Assert.Equal("Event 0 not found", err.Message);
      //  eventService.DeleteEventById(1);
     //   eventService.DeleteEventById(2);
        var all = eventService.GetEvents().ToList();
        Assert.Equal(all.Count, ExpectedTestData().Count() - 3);
    }

    [Fact]
    public void TestGetEventByIndex()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

   /*     var err = Assert.Throws<EventNotFoundException>(() => eventService.GetEventById(-1));
        Assert.Equal("Event -1 not found", err.Message);
        err = Assert.Throws<EventNotFoundException>(() => eventService.GetEventById(100));
        Assert.Equal("Event 100 not found", err.Message);
        
        var eventById = eventService.GetEventById(0);
        Assert.NotNull(eventById);
        var firstEvent = ExpectedTestData().First();
        Assert.Equal(firstEvent.Title, eventById.Title);
        
        eventById = eventService.GetEventById(ExpectedTestData().Count() - 1);
        Assert.NotNull(eventById);
        var lastEvent = ExpectedTestData().Last();
        Assert.Equal(lastEvent.Title, eventById.Title);*/
    }

    [Fact]
    public void TestGetAllEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        // Ожидаем существование четырёх записей [0-3]
        var events = eventService.GetEvents().ToList();
        Assert.Equal(ExpectedTestData().Count(), events.Count);
    }

    [Fact]
    public void TestGetAllWithFilterEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        // -------- Заголовки -------
        // Все по началу строки заголовка
        var targetCount = ExpectedTestData().Count();
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
        events = eventService.GetEvents(null, _date).ToList();
        Assert.Equal(targetCount, events.Count);
        events = eventService.GetEvents(null, _date - _offsetShort).ToList();
        Assert.Equal(targetCount, events.Count);
        // Одно событие, Test 6
        events = eventService.GetEvents(null, _date + _offsetShort).ToList();
        Assert.Single(events);
        // Никаких событий для поздней даты
        events = eventService.GetEvents(null, _date + _offsetLong).ToList();
        Assert.Empty(events);

        events = eventService.GetEvents(null, null, _date + _offsetLong).ToList();
        Assert.Equal(targetCount, events.Count);
        events = eventService.GetEvents(null, null, _date + _offsetShort).ToList();
        Assert.Equal(2, events.Count);
        events = eventService.GetEvents(null, null, _date).ToList();
        Assert.Empty(events);
        events = eventService.GetEvents(null, null, _date - _offsetShort).ToList();
        Assert.Empty(events);

        // Смешаные кейсы
        events = eventService.GetEvents("Tit", _date).ToList();
        Assert.Equal(targetCount - 1, events.Count);
        events = eventService.GetEvents("Tit", _date + _offsetLong).ToList();
        Assert.Empty(events);
        events = eventService.GetEvents("Tit", null, _date - _offsetLong).ToList();
        Assert.Empty(events);
        events = eventService.GetEvents("", _date, _date + _offsetLong).ToList();
        Assert.Equal(targetCount, events.Count);
    }

    [Fact]
    public void TestUpdateEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        var evs = UpdateTestData().ToList();
        eventService.UpdateEvent(evs[0].Item1.Id, evs[0].Item1);
        var err = Assert.Throws<EventValidationException>(() => eventService.UpdateEvent(evs[1].Item1.Id, evs[1].Item1));
        Assert.Equal(err.Message, messageInvalid);
        eventService.UpdateEvent(evs[2].Item1.Id, evs[2].Item1);    
        var err2 = Assert.Throws<EventNotFoundException>(() => eventService.UpdateEvent(evs[3].Item1.Id, evs[3].Item1));
        Assert.Equal("Event 100 not found", err2.Message);
    }

    [Fact]
    public void TestPageEvent()
    {
        var mockLogger = new Mock<ILogger<EventService>>();
        IEventService eventService = new EventService(mockLogger.Object);
        foreach (var ev in ExpectedPageTestData())
        {
            eventService.AddEvent(ev);
        }

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