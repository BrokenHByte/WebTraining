using Application.Bookings.DTOs;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByBookingId(Guid id)
    {
        var booking = await bookingService.GetByIdAsync(id);
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