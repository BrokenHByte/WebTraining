using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WebProject.Exceptions;
using WebProject.Repositories;
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
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly EventService _service;

    public TestEventService()
    {
        _mockRepository = new Mock<IEventRepository>();
        _mockLogger = new Mock<ILogger<EventService>>();
        _service = new EventService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task TestAddEvent()
    {
        var services = new ServiceCollection();

        var serviceProvider = services.BuildServiceProvider();
        var mockRepository = new Mock<IEventRepository>();
        mockRepository
            .Setup(repo => repo.GetEvents())
            .Returns(EventData.AddTestData().AsQueryable());

        var listData = EventData.AddTestData();
        var resultData = EventData.AddTestResult();

        for (int i = 0; i < listData.Count; i++)
        {
            if (resultData[i].Length > 0)
            {
                var err = await Assert.ThrowsAsync<EventValidationException>(async () =>
                    await _service.AddEventAsync(listData[i].Title, listData[i].Description, listData[i].StartAt,
                        listData[i].EndAt, 10));

                Assert.Equal(err.Message, resultData[i]);
            }
            else
            {
                await _service.AddEventAsync(listData[i].Title, listData[i].Description, listData[i].StartAt,
                    listData[i].EndAt, 10);
            }
        }
    }
}