using MongoDB.Bson.Serialization.Attributes;
namespace backend.Models;
public class User
{
    [BsonId] public Guid UserId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
