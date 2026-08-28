using Bookings.Application.Abstractions.Persistence.Repositories;
using MediatR;

namespace Bookings.Application.Bookings.Queries.GetBookingById;


public class GetBookingByIdHandler(IBookingRepository bookingRepository) : IRequestHandler<GetBookingByIdQuery, GetBookingByIdResponse>
{
   /* public async Task<GetBookingByIdResponse> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.Id);
        var user = await userService.Get(request.UserLogin);
        if (user == null)
            throw new InvalidOperationException($"User {request.UserLogin} not found");

        if (user.Role == User.Roles.User && booking.UserId != user.Id)
        {
            throw new InsufficientPrivilegesException("You do not have permission to get this booking");
        }

        return new GetBookingByIdResponse
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
  */
   public Task<GetBookingByIdResponse> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
   {
       throw new NotImplementedException();
   }
}
