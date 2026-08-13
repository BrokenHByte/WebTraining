using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Persistence.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Data.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerationToken(string login, User.Roles role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login),
            new(ClaimTypes.Email, login),
            new(ClaimTypes.Name, login),
            new(ClaimTypes.Role, role.ToString()),
        };

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