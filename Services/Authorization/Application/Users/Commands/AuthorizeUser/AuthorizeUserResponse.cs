namespace Application.Users.Commands.AuthorizeUser;

public record AuthorizeUserResponse
{
    public required string Token { get; init; }
}