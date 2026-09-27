using backend.Models;
using backend.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    // Temporary storage. Will be replaced with a database.
    private static readonly List<User> Users = new()
    {
        new User("Eden", "eden@example.com"),
        new User("Maya", "maya@example.com")
    };

    // GET /api/users
    // Return all users.
    [HttpGet]
    public IActionResult GetUsers()
    {
        return Ok(Users);
    }

    // GET /api/users/{id}
    // Return a specific user.
    [HttpGet("{id}")]
    public IActionResult GetUserById([FromRoute] Guid id)
    {
        User? user = Users.FirstOrDefault(u => u.UserId == id);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Ok(user);
    }

    // POST /api/users
    // Create a new user.
    [HttpPost]
    public IActionResult CreateUser(
        [FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Name and email are required.");
        }

        // Remove spaces from the beginning and end.
        string name = request.Name.Trim();
        string email = request.Email.Trim();

        // TODO: Validate the name and email format.

        User user = new User(name, email);
        Users.Add(user);

        // Return 201, the created user, and its retrieval URL.
        return CreatedAtAction(
            nameof(GetUserById),
            new { id = user.UserId },
            user);
    }

    // PUT /api/users/{id}/email
    // Update an existing user's email.
    [HttpPut("{id}/email")]
    public IActionResult UpdateEmail(
        [FromRoute] Guid id,
        [FromBody] UpdateUserEmailRequest request)
    {
        User? user = Users.FirstOrDefault(u => u.UserId == id);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email is required.");
        }

        // TODO: Validate the email format.

        user.Email = request.Email.Trim();

        return Ok(user);
    }
}