using Authorization.Application.Abstractions.Persistence.Common;
using Authorization.Application.Abstractions.Persistence.Services;
using Authorization.Domain.Entities;
using Authorization.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Authorization.Infrastructure.Data.Services;


public class UserService(AppDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService) : IUserService
{
    public async Task<User?> Get(string? login, User.Roles? role)
    {
        return await db.Users.Where(x => (login == null || x.Login == login) &&
                            (role == null || x.Role == role)).AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task Registration(string login, string password, User.Roles role)
    {
        var hash = passwordHasher.HashPassword(password);
        var result = await db.Users.Where(x => x.Login == login).FirstOrDefaultAsync();
        if (result != null)
            throw new LoginAlreadyUseException("Login already in use");
        await db.Users.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            HashPass = hash,
            Role = role
        });
        await db.SaveChangesAsync();
    }

    public async Task<string> Authorize(string login, string password)
    {
        var user = await db.Users.Where(x => x.Login == login).FirstOrDefaultAsync();
        if (user == null || !passwordHasher.VerifyPassword(password, user.HashPass))
            throw new InvalidCredentialsException("Invalid username or password");
        return tokenService.GenerationToken(user.Id.ToString(), login, user.Role);
    }

}