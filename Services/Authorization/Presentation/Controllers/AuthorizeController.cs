using Application.Users.Commands.AuthorizeUser;
using Application.Users.Commands.RegistrationUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[ApiController]
[Route("auth")]
public class AuthorizeController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthorizeUserResponse>> Login([FromBody] AuthorizeUserCommand data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await mediator.Send(data);
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistrationUserCommand data)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await mediator.Send(data);
        return NoContent();
    }

}