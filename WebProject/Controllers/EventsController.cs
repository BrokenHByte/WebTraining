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
    public async Task<IActionResult> GetAllAsync([FromQuery] string? title, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        int pageNumber = page ?? _defaultPage;
        int validPageSize = pageSize ?? _defaultSizePage;

        var events = (await eventService.GetEventsAsync(title, from, to)).ToList();

        var pageEvents = await eventService.GetPageAsync(events, pageNumber, validPageSize);
        var eventsDto = pageEvents.Select(o => new EventResponseDto
        {
            Id = o.Id,
            Title = o.Title,
            Description = o.Description,
            StartAt = o.StartAt,
            EndAt = o.EndAt,
            TotalSeats = o.TotalSeats,
            AvailableSeats = o.AvailableSeats
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
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        var oneEvent = await eventService.GetEventByIdAsync(id);

        var eventsDto = new EventResponseDto
        {
            Id = oneEvent.Id,
            Title = oneEvent.Title,
            Description = oneEvent.Description,
            StartAt = oneEvent.StartAt,
            EndAt = oneEvent.EndAt,
            TotalSeats = oneEvent.TotalSeats,
            AvailableSeats = oneEvent.AvailableSeats
        };

        return Ok(eventsDto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEventAsync([FromBody] EventCreateDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await eventService.AddEventAsync(
            data.Title,
            data.Description,
            data.StartAt,
            data.EndAt,
            data.TotalSeats
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
    public async Task<IActionResult> UpdateEventAsync(Guid id, [FromBody] EventCreateDto data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var oneEvent = new Event
        {
            Id = id,
            Title = data.Title,
            Description = data.Description,
            StartAt = data.StartAt,
            EndAt = data.EndAt,
            TotalSeats = data.TotalSeats
        };

        await eventService.UpdateEventAsync(id, oneEvent);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEventAsync(Guid id)
    {
        await eventCoordinationService.DeleteEventWithCheck(id);
        return Ok();
    }
}