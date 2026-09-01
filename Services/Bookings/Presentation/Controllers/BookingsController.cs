using System.Security.Claims;
using Bookings.Application.Bookings.Commands.CreateBooking;
using Bookings.Application.Bookings.Commands.DeleteBooking;
using Bookings.Application.Bookings.Queries.GetBookingById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingsController(IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost("{eventId}/book")]
    public async Task<ActionResult<CreateBookingResponse>> CreateBookingAsync(Guid eventId)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        CreateBookingResponse booking = await mediator.Send(new CreateBookingCommand()
        {
            EventId = eventId.ToString(),
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException(),
            UserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? throw new InvalidOperationException(),
        });
        Response.Headers.Location = $"/bookings/{booking.Id}";
        return Accepted(booking);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBookingAsync(Guid id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await mediator.Send(new CancelBookingCommand()
        {
            Id = id,
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException(),
            UserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? throw new InvalidOperationException()
        });
        return Ok();
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<GetBookingByIdResponse>> GetByBookingId(Guid id)
    {
        var booking = await mediator.Send(new GetBookingByIdQuery()
        {
            Id = id,
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException(),
            UserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? throw new InvalidOperationException()
        });
        return Ok(booking);
    }
}