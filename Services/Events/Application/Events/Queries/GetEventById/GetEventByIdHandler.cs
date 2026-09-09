using System.Text.Json;
using Events.Application.Abstractions.Persistence.Repositories;
using Events.Application.Abstractions.Persistence.Services;
using Events.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Events.Application.Events.Queries.GetEventById;

public class GetEventByIdHandler(IEventRepository eventRepository, ICacheService cacheService) : IRequestHandler<GetEventByIdQuery, GetEventByIdResponse>
{
    public async Task<GetEventByIdResponse> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheValue = await cacheService.GetObjectJson<GetEventByIdResponse>($"event:{request.Id}");
        if (cacheValue != null)
        {
            return cacheValue;
        }

        var result = await eventRepository.GetByIdAsync(request.Id);
        var obj = new GetEventByIdResponse()
        {
            Id = result.Id,
            Title = result.Title,
            Description = result.Description,
            StartAt = result.StartAt,
            EndAt = result.EndAt,
            AvailableSeats = result.AvailableSeats,
            TotalSeats = result.TotalSeats
        };
        await cacheService.SetObjectJson($"event:{request.Id}", obj);
        return obj;
    }
}
