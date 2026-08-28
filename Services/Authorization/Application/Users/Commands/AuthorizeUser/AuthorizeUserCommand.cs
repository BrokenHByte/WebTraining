using MediatR;

namespace Application.Users.Commands.AuthorizeUser;

public sealed record AuthorizeUserCommand : IRequest<AuthorizeUserResponse>
{
    public required string Login { get; init; }
    public required string Password { get; init; }
}