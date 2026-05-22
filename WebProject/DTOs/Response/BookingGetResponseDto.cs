using WebProject.Models;

namespace WebProject.DTOs.Response;

public class BookingGetResponseDto
{
    public Booking.BookingStatus Status { get; set; }
}