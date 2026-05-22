using Microsoft.AspNetCore.Mvc;
using WebProject.Services;

namespace WebProject.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByBookingId(Guid bookingId)
    {
        var booking = await bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null) return NotFound();
        return Ok(booking.Status);
    }
}