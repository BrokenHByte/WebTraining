using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebTraining.DTOs;
using WebTraining.Models;
using WebTraining.Services;

namespace WebTraining.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private IEventService _eventService = eventService;

    [HttpGet]
    public IActionResult GetAll()
    {
        var events = _eventService.GetEvents();
        var eventsDto = events.Select(o => new EventResponseDto
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartAt = o.StartAt,
            EndAt = o.EndAt
        });

        return Ok(eventsDto);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var oneEvent = _eventService.GetEventById(id);
        if (oneEvent == null)
            return NotFound();

        var eventsDto = new EventResponseDto
        {
            Id = oneEvent.Id,
            Title = oneEvent.Title,
            Description = oneEvent.Description,
            StartAt = oneEvent.StartAt,
            EndAt = oneEvent.EndAt
        };
        return Ok(eventsDto);
    }

    [HttpPost]
    public IActionResult CreateEvent([FromBody] EventCreateDto data)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (_eventService.AddEvent(new Event
            {
                Title = data.Title,
                Description = data.Description,
                StartAt = data.StartAt,
                EndAt = data.EndAt
            }))
            return Created();

        return Conflict("Указанный Id уже существует");
    }

    [HttpPut("{id}")]
    public IActionResult UpdateEvent(int id, [FromBody] EventCreateDto data)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var oneEvent = new Event
        {
            Id = id,
            Title = data.Title,
            Description = data.Description,
            StartAt = data.StartAt,
            EndAt = data.EndAt
        };

        if (_eventService.UpdateEvent(id, oneEvent))
            return Ok();
        return NotFound();
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(int id)
    {
        if (_eventService.DeleteEventById(id))
            return Ok();
        return NotFound();
    }
}