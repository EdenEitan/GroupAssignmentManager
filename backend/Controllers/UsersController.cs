using backend.Contracts;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Mvc;
namespace backend.Controllers;
[ApiController, Route("api/users")]
public class UsersController(UserRepository users) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> All() => Ok(await users.All());
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) =>
        await users.Get(id) is { } user ? Ok(user) : NotFound("User not found.");
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email)
            || !System.Net.Mail.MailAddress.TryCreate(request.Email.Trim(), out _))
            return BadRequest("Provide a name and a valid email.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmail(email) is not null) return Conflict("Email already exists.");
        var user = new User { Name = request.Name.Trim(), Email = email };
        await users.Add(user);
        return CreatedAtAction(nameof(Get), new { id = user.UserId }, user);
    }
}
