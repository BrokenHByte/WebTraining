using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WebProject.DTOs;
using WebProject.Models;
using WebProject.Services;

namespace WebProject.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(ILogger<EventsController> logger, IEventService eventService) : ControllerBase
{
    private readonly int _defaultPage = 1;
    private readonly int _defaultSizePage = 10;
    private IEventService _eventService = eventService;

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var pageNumber = page ?? _defaultPage;
        var validPageSize = pageSize ?? _defaultSizePage;

        var events = _eventService.GetEvents(title, from, to).ToList();

        var pageEvents = _eventService.GetPage(events, pageNumber, validPageSize);
        var eventsDto = pageEvents.Select(o => new EventResponseDto
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartAt = o.StartAt,
            EndAt = o.EndAt
        }).ToList();

        var eventsPaginated = new EventPaginatedResultDto()
        {
            TotalCountEvents = events.Count,
            CurrentPage = pageNumber,
            PageSize = eventsDto.Count,
            Events = eventsDto
        };
        return Ok(eventsPaginated);
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