using WebProject.Models;
using WebProject.Services;

namespace Tests;

public class TestEventService
{
    private readonly DateTime _date = new(1989, 10, 07);
    private readonly TimeSpan _offsetShort = TimeSpan.FromHours(1);
    private readonly TimeSpan _offsetLong = TimeSpan.FromHours(2);

    // Данные для проверки вставки
    IEnumerable<(Event, bool)> AddTestData()
    {
        return
        [
            (new Event { Title = "Title1", Description = "Test1", StartAt = _date + _offsetShort, EndAt = _date },
                false),
            (new Event { Title = "Title2", Description = "Test2", StartAt = _date, EndAt = _date + _offsetShort },
                true),
            (new Event { Title = "Title3", Description = null, StartAt = _date, EndAt = _date }, false),
            (new Event { Title = "Title4", Description = "Test4", StartAt = _date, EndAt = _date + _offsetShort },
                true),
            (new Event { Title = "", Description = "Test5", StartAt = _date, EndAt = _date + _offsetLong }, true),
            (new Event { Title = "Title6", Description = "Test6", StartAt = _date + _offsetShort, EndAt = _date + _offsetLong },
                true),
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
    IEnumerable<(Event, bool)> UpdateTestData()
    {
        return
        [
            (new Event { Id = 0, Title = "NewTitle2", Description = "Test2", StartAt = _date, EndAt = _date + _offsetShort },
                true),
            (new Event { Id = 1, Title = "NewTitle4", Description = "Test4", StartAt = _date + _offsetShort, EndAt = _date },
                false),
            (new Event { Id = 2, Title = "", Description = null, StartAt = _date, EndAt = _date + _offsetLong }, true),
            (new Event { Id = 100, Title = "NewTitle6", Description = "Test6", StartAt = _date + _offsetShort, EndAt = _date + _offsetLong },
                false),
        ];
    }

    static IEnumerable<(int, bool)> TestDeleteData()
    {
        return
        [
            (1, true),
            (2, true),
            (2, false),
            (3, true)
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
        IEventService eventService = new EventService();
        foreach (var ev in AddTestData())
        {
            var result = eventService.AddEvent(ev.Item1);
            if (result != ev.Item2)
            {
                Assert.Fail("Error in the data test: Title = " + ev.Item1.Title);
            }
        }
    }

    [Fact]
    public void TestDeleteEvent()
    {
        IEventService eventService = new EventService();
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        // Ожидаем существование четырёх записей [0-3]
        var result = eventService.DeleteEventById(0);
        Assert.True(result);
        result = eventService.DeleteEventById(0);
        Assert.False(result);
        result = eventService.DeleteEventById(1);
        Assert.True(result);
        result = eventService.DeleteEventById(2);
        Assert.True(result);
    }

    [Fact]
    public void TestGetEventByIndex()
    {
        IEventService eventService = new EventService();
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        var eventById = eventService.GetEventById(-1);
        Assert.Null(eventById);
        eventById = eventService.GetEventById(0);
        Assert.NotNull(eventById);
        var firstEvent = ExpectedTestData().First();
        Assert.Equal(firstEvent.Title, eventById.Title);
        eventById = eventService.GetEventById(100);
        Assert.Null(eventById);
    }

    [Fact]
    public void TestGetAllEvent()
    {
        IEventService eventService = new EventService();
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        // Ожидаем существование четырёх записей [0-3]
        var events = eventService.GetEvents().ToList();
        Assert.Equal(AddTestData().Count(x => x.Item2), events.Count);
    }

    [Fact]
    public void TestGetAllWithFilterEvent()
    {
        IEventService eventService = new EventService();
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        // -------- Заголовки -------
        // Все по началу строки заголовка
        var targetCount = AddTestData().Count(x => x.Item2);
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
        IEventService eventService = new EventService();
        foreach (var ev in ExpectedTestData())
        {
            eventService.AddEvent(ev);
        }

        foreach (var ev in UpdateTestData())
        {
            var result = eventService.UpdateEvent(ev.Item1.Id, ev.Item1);
            if (result != ev.Item2)
            {
                Assert.Fail("Error in the data test: Title = " + ev.Item1.Title);
            }
        }
    }

    [Fact]
    public void TestPageEvent()
    {
        IEventService eventService = new EventService();
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