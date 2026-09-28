using MongoDB.Bson.Serialization.Attributes;
namespace backend.Models;
public class Project
{
    [BsonId] public Guid ProjectId { get; set; } = Guid.NewGuid();
    public string ProjectName { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? Deadline { get; set; }
    public List<Guid> MemberIds { get; set; } = new();
}
