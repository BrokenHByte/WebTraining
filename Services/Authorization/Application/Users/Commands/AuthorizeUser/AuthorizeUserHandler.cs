using Application.Abstractions.Persistence.Services;
using Application.Users.Commands.RegistrationUser;
using MediatR;

namespace Application.Users.Commands.AuthorizeUser;

public class AuthorizeUserHandler(IUserService userService) : IRequestHandler<AuthorizeUserCommand, AuthorizeUserResponse>
{
    public async Task<AuthorizeUserResponse> Handle(AuthorizeUserCommand request, CancellationToken cancellationToken)
    {
        var token = await userService.Authorize(request.Login, request.Password);
        return new AuthorizeUserResponse
        {
            Token = token,
        };
    }
}