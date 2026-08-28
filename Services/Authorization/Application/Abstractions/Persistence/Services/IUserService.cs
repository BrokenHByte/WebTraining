using Domain.Entities;

namespace Application.Abstractions.Persistence.Services;

public interface IUserService
{
    Task<User?> Get(string? login = null, User.Roles? role = null);
    Task Registration(string login, string password, User.Roles role);
    Task<string> Authorize(string login, string password);
}
