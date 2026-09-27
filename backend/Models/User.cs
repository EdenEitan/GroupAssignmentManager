namespace backend.Models;

public class User
{
    // set when the user is created. Cannot be changed later.
    // next i will set uniqunes check with the DB. 
    public Guid UserId { get; }

    // Name changes are not supported for now.
    public string Name { get; }

    // can be read and updated after the user is created.
    public string Email { get; set; }

    // Name and email validation will be done on the server
    // when handling the request, before creating the user.
    public User(string name, string email)
    {
        UserId = Guid.NewGuid();
        Name = name;
        Email = email;
    }
}