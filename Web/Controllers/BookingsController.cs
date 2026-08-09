using Application.Bookings.Commands.CreateBooking;
using Application.Bookings.Queries.GetBookingById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingsController(IMediator mediator) : ControllerBase
{
    
    [HttpPost("{eventId}/book")]
    public async Task<ActionResult<CreateBookingResponse>> CreateBookingAsync(Guid eventId)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        CreateBookingResponse booking = await mediator.Send(new CreateBookingCommand()
        {
            EventId = eventId
        });
        Response.Headers.Location = $"/bookings/{booking.Id}";
        return Accepted(booking);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<GetBookingByIdResponse>> GetByBookingId(Guid id)
    {
        var booking = await mediator.Send(new GetBookingByIdQuery()
        {
            Id = id
        });
        return Ok(booking);
    }
}