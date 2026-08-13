using System.IdentityModel.Tokens.Jwt;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Persistence.Repositories;
using Application.Abstractions.Persistence.Services;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Data.Repositories;


public class UserService(AppDbContext db, IConfiguration configuration): IUserService
{
    public async Task<User?> Get(string? login, User.Roles? role)
    {
        return await db.Users.Where(x => (login == null || x.Login == login) &&
                            (role == null || x.Role == role)).AsNoTracking().FirstOrDefaultAsync();
    }

    public async Task Registration(string login, string password, User.Roles role)
    {
        var hash = CalculatePasswordHash(password);
        var result = await db.Users.Where(x => x.Login == login && x.HashPass == hash).FirstOrDefaultAsync();
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
        var hash = CalculatePasswordHash(password);
        var result = await db.Users.Where(x => x.Login == login && x.HashPass == hash).FirstOrDefaultAsync();
        if (result == null)
            throw new SecurityException("Invalid username or password");
        return GenerationToken(login, password, result.Role);
    }
    
    public string CalculatePasswordHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes); 
    }

    string GenerationToken(string login, string password, User.Roles role)
    {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, login),
                new(ClaimTypes.Email, login),
                new(ClaimTypes.Name, login),
                new(ClaimTypes.Role, role.ToString()),
            };

            // Получаем ключ из конфигурации
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException())
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                configuration["Jwt:Issuer"],
                configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
    }
}