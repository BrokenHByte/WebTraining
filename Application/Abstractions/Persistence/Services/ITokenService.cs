using Domain.Entities;

namespace Application.Abstractions.Persistence.Services;

public interface ITokenService
{
    string GenerationToken(string login, User.Roles role);
}