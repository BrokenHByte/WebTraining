using WebProject.Models;

namespace Tests;

public class EventData
{
    public static string messageInvalid = "Event with id is invalid: EndAt <= StartAt";
    public static DateTime dateExample = new(1989, 10, 07);
    public static TimeSpan OffsetShort = TimeSpan.FromHours(1);
    public static TimeSpan OffsetLong = TimeSpan.FromHours(2);

    // Данные для проверки вставки
    public static List<Event> AddTestData()
    {
        return
        [
            new Event
            {
                Id = Guid.NewGuid(), Title = "Title1", Description = "Test1", StartAt = dateExample + OffsetShort,
                EndAt = dateExample
            },

            new Event
            {
                Id = Guid.NewGuid(), Title = "Title2", Description = "Test2", StartAt = dateExample,
                EndAt = dateExample + OffsetShort
            },

            new Event
            {
                Id = Guid.NewGuid(), Title = "Title3", Description = null, StartAt = dateExample, EndAt = dateExample
            },

            new Event
            {
                Id = Guid.NewGuid(), Title = "Title4", Description = "Test4", StartAt = dateExample,
                EndAt = dateExample + OffsetShort
            },

            new Event
            {
                Id = Guid.NewGuid(), Title = "", Description = "Test5", StartAt = dateExample,
                EndAt = dateExample + OffsetLong
            },

            new Event
            {
                Id = Guid.NewGuid(), Title = "Title6", Description = "Test6", StartAt = dateExample + OffsetShort,
                EndAt = dateExample + OffsetLong
            }
        ];
    }

    public static List<string> AddTestResult()
    {
        return
        [
            messageInvalid, "", messageInvalid, "", "", ""
        ];
    }


    // Произвольный набор валидных данных
    public static List<Event> ExpectedTestData()
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
    public static List<(Event, string)> UpdateTestData()
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

    public static List<(int, string)> TestDeleteData()
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
    public static List<Event> ExpectedPageTestData()
    {
        var result = new List<Event>();

        for (int i = 0; i < 100; i++)
        {
            result.Add(new Event
            {
                Title = $"Title{i + 1}", Description = $"Test{i + 1}", StartAt = dateExample,
                EndAt = dateExample + OffsetShort
            });
        }

        return result;
    }
}