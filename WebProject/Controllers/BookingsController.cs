using Microsoft.AspNetCore.Mvc;
using WebProject.DTOs.Response;
using WebProject.Services;

namespace WebProject.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByBookingId(Guid id)
    {
        var booking = await bookingService.GetBookingByIdAsync(id);
        if (booking == null) return NotFound();
        var bookingDto = new BookingGetResponseDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
        return Ok(bookingDto);
    }
}