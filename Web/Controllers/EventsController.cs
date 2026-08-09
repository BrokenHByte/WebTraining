using Application.Events.Commands.CreateEvent;
using Application.Events.Commands.DeleteEvent;
using Application.Events.Commands.UpdateEvent;
using Application.Events.Queries.GetEventById;
using Application.Events.Queries.GetEventsPage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(
    IMediator mediator) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<GetEventByIdResponse>> GetAllAsync([FromQuery] string? title, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var result = await mediator.Send(new GetEventPageQuery(){ Title = title,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize});
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetEventByIdResponse>> GetByIdAsync(Guid id)
    {
        var result = await mediator.Send(new GetEventByIdQuery(){ Id = id});
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateEventCommand data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var guid = await mediator.Send(data);
        return Created();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEventAsync([FromBody] UpdateEventCommand data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await mediator.Send(data);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEventAsync([FromBody] DeleteEventCommand data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await mediator.Send(data);
        return Ok();
    }
}