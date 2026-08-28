using Authorization.Domain.Entities;

namespace Authorization.Application.Abstractions.Persistence.Services;

public interface ITokenService
{
    string GenerationToken(string login, User.Roles role);
}