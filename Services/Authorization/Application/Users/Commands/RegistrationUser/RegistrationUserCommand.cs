using Domain.Entities;
using MediatR;

namespace Application.Users.Commands.RegistrationUser;

public sealed record RegistrationUserCommand : IRequest
{
    public required string Login { get; init; }
    public required string Password { get; init; }
    public User.Roles Role { get; init; }
}