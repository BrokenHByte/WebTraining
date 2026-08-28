using Application.Abstractions.Persistence.Services;
using MediatR;

namespace Application.Users.Commands.RegistrationUser;

public class RegistrationUserHandler(IUserService userService) : IRequestHandler<RegistrationUserCommand>
{
    public async Task Handle(RegistrationUserCommand request, CancellationToken cancellationToken)
    {
        await userService.Registration(request.Login, request.Password, request.Role);
    }
}