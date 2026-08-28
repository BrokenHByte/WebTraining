namespace Application.Common.Config;

public class BookingSettings
{
    // Лимит бронирования для одного пользователя
    public int PerUserLimit { get; set; } = 10;
}