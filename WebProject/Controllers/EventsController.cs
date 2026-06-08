using Microsoft.AspNetCore.Mvc;
using WebProject.DTOs;
using WebProject.DTOs.Response;
using WebProject.Models;
using WebProject.Services;

namespace WebProject.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController(
    IEventService eventService,
    IEventCoordinationService eventCoordinationService,
    IBookingService bookingService) : ControllerBase
{
    private readonly int _defaultPage = 1;
    private readonly int _defaultSizePage = 10;

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? title, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var pageNumber = page ?? _defaultPage;
        var validPageSize = pageSize ?? _defaultSizePage;

        var events = eventService.GetEvents(title, from, to).ToList();

        var pageEvents = eventService.GetPage(events, pageNumber, validPageSize);
        var eventsDto = pageEvents.Select(o => new EventResponseDto
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartAt = o.StartAt,
            EndAt = o.EndAt
        }).ToList();

        var eventsPaginated = new EventPaginatedResponseDto
        {
            TotalCountEvents = events.Count,
            CurrentPage = pageNumber,
            PageSize = eventsDto.Count,
            Events = eventsDto
        };
        return Ok(eventsPaginated);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var oneEvent = eventService.GetEventById(id);

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
            return BadRequest(ModelState);

        eventService.AddEvent(
            data.Title,
            data.Description,
            data.StartAt,
            data.EndAt
        );
        return Created();
    }

    [HttpPost("{id}/book")]
    public async Task<IActionResult> CreateBookingAsync(Guid id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var booking = await bookingService.CreateBookingAsync(id);
        var response = new BookingCreateResponseDto { Id = booking.Id, EventId = id, Status = booking.Status };
        Response.Headers.Location = $"/bookings/{booking.Id}";
        return Accepted(response);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateEvent(Guid id, [FromBody] EventCreateDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var oneEvent = new Event
        {
            Id = id,
            Title = data.Title,
            Description = data.Description,
            StartAt = data.StartAt,
            EndAt = data.EndAt
        };

        eventService.UpdateEvent(id, oneEvent);
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteEvent(Guid id)
    {
        eventCoordinationService.DeleteEventWithCheck(id);
        return Ok();
    }
}