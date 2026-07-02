using WebProject.Models;

namespace Tests;

public class EventData
{
    public static string messageInvalid = "Event with id is invalid: EndAt <= StartAt";
    public static DateTime dateExample = new(1989, 10, 07);
    public static TimeSpan OffsetShort = TimeSpan.FromHours(1);
    public static TimeSpan OffsetLong = TimeSpan.FromHours(2);

    // Данные для проверки вставки
    public static IEnumerable<(Event, string)> AddTestData()
    {
        return
        [
            (new Event { Title = "Title1", Description = "Test1", StartAt = dateExample + OffsetShort, EndAt = dateExample },
                messageInvalid),
            (new Event { Title = "Title2", Description = "Test2", StartAt = dateExample, EndAt = dateExample + OffsetShort },
                ""),
            (new Event { Title = "Title3", Description = null, StartAt = dateExample, EndAt = dateExample },
                messageInvalid),
            (new Event { Title = "Title4", Description = "Test4", StartAt = dateExample, EndAt = dateExample + OffsetShort },
                ""),
            (new Event { Title = "", Description = "Test5", StartAt = dateExample, EndAt = dateExample + OffsetLong },
                ""),
            (new Event { Title = "Title6", Description = "Test6", StartAt = dateExample + OffsetShort, EndAt = dateExample + OffsetLong },
                "")
        ];
    }

    // Произвольный набор валидных данных
    public static IEnumerable<Event> ExpectedTestData()
    {
        return
        [
            new Event
            {
                Title = "Title2", Description = "Test2", StartAt = dateExample, EndAt = dateExample + OffsetShort
            },
            new Event
            {
                Title = "Title4", Description = "Test4", StartAt = dateExample, EndAt = dateExample + OffsetShort
            },
            new Event { Title = "", Description = "Test5", StartAt = dateExample, EndAt = dateExample + OffsetLong },
            new Event
            {
                Title = "Title6", Description = "Test6", StartAt = dateExample + OffsetShort,
                EndAt = dateExample + OffsetLong
            }
        ];
    }

    // Обновление для ExpectedTestData набора
    public static IEnumerable<(Event, string)> UpdateTestData()
    {
        return
        [
            (new Event { Title = "NewTitle2", Description = "Test2", StartAt = dateExample, EndAt = dateExample + OffsetShort },
                ""),
            (new Event { Title = "NewTitle4", Description = "Test4", StartAt = dateExample + OffsetShort, EndAt = dateExample },
                messageInvalid),
            (new Event { Title = "", Description = null, StartAt = dateExample, EndAt = dateExample + OffsetLong },
                ""),
            (new Event { Title = "NewTitle6", Description = "Test6", StartAt = dateExample + OffsetShort, EndAt = dateExample + OffsetLong },
                "Event not found")
        ];
    }

    public static IEnumerable<(int, string)> TestDeleteData()
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
    public static IEnumerable<Event> ExpectedPageTestData()
    {
        for (var i = 0; i < 100; i++)
            yield return new Event
            {
                Title = $"Title{i + 1}", Description = $"Test{i + 1}", StartAt = dateExample,
                EndAt = dateExample + OffsetShort
            };
    }
}