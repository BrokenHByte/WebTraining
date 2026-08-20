using Application.Bookings.Commands.CreateBooking;
using Domain.Entities;
using MediatR;

namespace Application.Bookings.Commands.DeleteBooking;

public sealed record CancelBookingCommand : IRequest
{
    public Guid Id { get; init; }
    public required string UserLogin { get; init; }
}