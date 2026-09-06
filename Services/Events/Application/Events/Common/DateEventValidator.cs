using Events.Domain.Exceptions;

namespace Events.Application.Events.Common;

public static class DateEventValidator
{
    public static void Check(DateTime startAt, DateTime endAt)
    {
        if (startAt >= endAt)
            throw new EventValidationException("Event with id is invalid: EndAt <= StartAt");
    }
}